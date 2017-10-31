Here's some infrastructure work done for a Unit of Work/Repository application domain modeling framework that wraps EF, done for a WPF application.

All model objects that implement IModelObject are hooked into the Unit of Work that wraps the DbContext, handling any errors throws on a validation error, updating any WPF UI binding.

A rudimentary cancel/undo changes feature was added to the Unit of Work.
