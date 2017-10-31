#### Explanation
Synchronization object context matches updates target DB context what's loaded into source DB context after saving changes.

#### Description

The ObjectContext class used by DbSync is not publicly supported by the Entity Framework team.
It's risky, but let's look at what it gets us...

This component was made for a very specific use case and wasn't intended to be maintained.
Not all use cases have been fully tested, albeit quite a variety have been convered.
That said, we're getting into into some really low-level stuff that's meant to present intimate knowledge about the framework.

###### *Handles property, reference, collection, and state changes.*

```
var source = new DbContext();
var target = new DbContext();

var foo1A = source.Set<Foo>().Find(1);
var foo2A = source.Set<Foo>().Add(new Foo());

var foo1B = target.Set<Foo>().Find(1);

foo1A.Name = "QUX";

var syncFactory = new DbSyncFactory();
var sync = sync.Create(source, target);

source.SaveChanges();

sync.Sync();    // attach inserted, detach deleted 
sync.Refresh(); // refresh inserted, deleted, modified
sync.Flush();   // update sync states for all entites loaded into sync context

Assert.Equals(foo1A.Name, foo1B.Name);

// client wins (already loaded)

var foo2B = target.Set<Foo>().Find(foo2A.Id);
```