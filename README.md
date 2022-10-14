# Druware.Server

The base/foundation library of all things Druware.Server

More details to be added as this comes together into something more than the
placeholder it currently is.

As implemented, the Models and Controllers that are included in this library
will be published by default in any API or Web application the library is
included in.  While the migrations are included, the will need to be called.
For convenience there is a MigrationManager built into the library that can
be overridden, or called from the parent.

In addition, the Context used for generating the migrations is configured
for PostgreSQL though it can be altered to use SQL Server.  The migration
should, in theory at least, detect the connection type, and select the right
migration based upon that, it is not yet heavily tested though.


## Dependencies

Druware.Extensions
Microsoft.Extensions.Configuration
Microsoft.Extensions.Configuration.Binder
