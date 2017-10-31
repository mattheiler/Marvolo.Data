using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marvolo.Data.Extensions;
using Marvolo.Data.Threading;

namespace Marvolo.Data
{
    public class ModelObjectWorkspace<TContext> : IModelObjectWorkspace where TContext : DbContext
    {
        protected ModelObjectWorkspace(TContext context)
        {
            _context = context;
        }

        private TContext _context { get; }

        private AsyncLock _monitor { get; } = new AsyncLock();

        public event EventHandler AcceptedChanges;

        public event EventHandler RejectedChanges;

        public void Dispose()
        {
            _context.Dispose();
        }

        public bool HasChanges()
        {
            return _context.ChangeTracker.HasChanges();
        }

        public bool Save()
        {
            try
            {
                _context.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                HandleDbEntityValidationException(e);
                return false;
            }
            catch (DbUpdateException e)
            {
                HandleDbUpdateException(e);
                return false;
            }

            AcceptedChanges?.Invoke(this, EventArgs.Empty);

            return true;
        }

        public Task<bool> SaveAsync()
        {
            return SaveAsync(CancellationToken.None);
        }

        public async Task<bool> SaveAsync(CancellationToken token)
        {
            using (await _monitor.LockAsync())
            {
                try
                {
                    await _context.SaveChangesAsync(token);
                }
                catch (DbEntityValidationException e)
                {
                    HandleDbEntityValidationException(e);
                    return false;
                }
                catch (DbUpdateException e)
                {
                    HandleDbUpdateException(e);
                    return false;
                }

                AcceptedChanges?.Invoke(this, EventArgs.Empty);

                return true;
            }
        }

        public void Undo()
        {
            _context.GetObjectContext().RejectChanges();

            // invalidate? if the pattern is to use backing fields and setters for property change notifications... no. use the strategy pattern? ...per entity? context?

            RejectedChanges?.Invoke(this, EventArgs.Empty);
        }

        public async Task UndoAsync()
        {
            using (await _monitor.LockAsync())
            {
                Undo();
            }
        }

        protected virtual void HandleDbUpdateException(DbUpdateException e)
        {
            foreach (var entry in e.Entries)
                if (entry.Entity is IModelObject entity)
                    entity.ErrorInfo.Add(new ModelObjectError(e.Message)); // translator service? map to SQL error code from inner SQL exception?
        }

        protected virtual void HandleDbEntityValidationException(DbEntityValidationException e)
        {
            foreach (var result in e.EntityValidationErrors)
                if (result.Entry.Entity is IModelObject entity)
                    foreach (var errors in result.ValidationErrors.GroupBy(error => error.PropertyName))
                        entity.ErrorInfo.AddRange(errors.Select(error => new ModelObjectError(errors.Key, error.ErrorMessage)));
        }
    }
}