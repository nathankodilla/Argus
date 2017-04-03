# Argus

## Usage
Change your Entity Framework DbContext class to inherit from `Argus.EntityFramework.ArgusDbContext`. 

For example:
```c#
public class SchoolContext : Argus.EntityFramework.ArgusDbContext
{
  public DbSet<Course> Courses { get; set; }
  public DbSet<Enrollment> Enrollments { get; set; }
  public DbSet<Student> Students { get; set; }
}
```

## How it works
The library intercepts calls to `SaveChanges` & `SaveChangesAsync` methods on the `DbContext` and generates an audit event that includes information of the affected entities based on configuration.

## Configuration

### Settings
The following settings can be configured per DbContext or globally:

- **Mode**: To indicate the member mode
  - _Opt-Out_: All the entities are tracked by default, except those explicitly ignored. (Default)
  - _Opt-In_: No entity is tracked by default, except those explicitly included.

Change the settings by decorating your DbContext with the `AuditDbContext` attribute, for example:

```c#
[AuditDbContext(Mode = MemberMode.OptIn, IncludeDatabase = false, IncludeConnectionID = false, IncludeTransactionID = false)]
public class SchoolContext : Argus.EntityFramework.ArgusDbContext
{
...
```

To exclude specific entities from the audit (OptOut Mode), you can decorate your DbSet properties with the `AuditIgnore` attribute, for example:
```c#
[AuditDbContext(Mode = MemberMode.OptOut)]
public class SchoolContext : Argus.EntityFramework.ArgusDbContext
{
  [AuditIgnore]
  public DbSet<Course> Courses { get; set; }
  ...
```

Instead, to include specific entities to the audit (OptIn Mode), you can use the `AuditInclude` attribute:
```c#
[AuditDbContext(Mode = MemberMode.OptIn)]
public class SchoolContext : Argus.EntityFramework.ArgusDbContext
{
  [AuditInclude]
  public DbSet<Course> Courses { get; set; }
  ...
```

To track only certain database operations:
```c#
[AuditDbContext(Mode = MemberMode.OptIn)]
public class SchoolContext : Argus.EntityFramework.ArgusDbContext
{
  [AuditInclude]
  [AuditDatabaseActions(DatabaseAction.Update)]
  public DbSet<Course> Courses { get; set; }
  ...
```

## Output samples (stored in MongoDB)
- Insert
```javascript
{
   "_t":"EntityFrameworkAuditEvent",
   "DatabaseActions":[
      {
         "Table":"Categories",
         "PrimaryKey":{
            "CategoryId":50
         },
         "DatabaseAction":"Insert",
         "ColumnValues":{
            "CategoryId":50,
            "CategoryName":"Test Category",
            "Description":"Test description",
            "Picture":null
         }
      }
   ]
}
```

- Update
```javascript
{
   "_t":"EntityFrameworkAuditEvent",
   "DatabaseActions":[
      {
         "Table":"Categories",
         "PrimaryKey":{
            "CategoryId":50
         },
         "DatabaseAction":"Update",
         "ColumnValues":{
            "Description":{
               "OriginalValue":"Test description",
               "NewValue":"D2"
            }
         }
      }
   ]
}
```
