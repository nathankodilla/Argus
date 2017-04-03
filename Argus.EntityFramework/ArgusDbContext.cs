using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;

namespace Argus.EntityFramework
{
	public abstract class ArgusDbContext : DbContext
	{
		private static readonly Dictionary<Type, bool?> IncludeIgnoreAttributeCache = new Dictionary<Type, bool?>();
		private static readonly Dictionary<Type, DatabaseAction?> ActionsAttributeCache = new Dictionary<Type, DatabaseAction?>();
		private static readonly Dictionary<Type, AuditDbContextAttribute> AuditAttributeCache = new Dictionary<Type, AuditDbContextAttribute>();
		private static readonly Dictionary<PropertyInfo, bool?> IgnoreUpdateAttributeCache = new Dictionary<PropertyInfo, bool?>();
		private static readonly Dictionary<Type, IEnumerable<IProperty>> EntityPropertiesCache = new Dictionary<Type, IEnumerable<IProperty>>();


		internal EntityFrameworkAuditSettings Settings = new EntityFrameworkAuditSettings();
		internal ArgusOptions ArgusOptions = new ArgusOptions();

		public bool AuditDisabled { get; set; }
		public Dictionary<string, object> ExtraFields { get; } = new Dictionary<string, object>();

		protected ArgusDbContext(Action<ArgusOptions> argusOptions)
		{
			argusOptions.Invoke(ArgusOptions);
			LoadConfig();
		}

		protected ArgusDbContext(DbContextOptions options, Action<ArgusOptions> argusOptions) : base(options)
		{
			argusOptions.Invoke(ArgusOptions);
			LoadConfig();
		}

		private void LoadConfig()
		{
			Type type = GetType();
			AuditDbContextAttribute attribute = null;

			if (AuditAttributeCache.ContainsKey(type))
			{
				attribute = AuditAttributeCache[type];
			}
			else
			{
				attribute = type.GetTypeInfo().GetCustomAttribute<AuditDbContextAttribute>();
				AuditAttributeCache[type] = attribute;
			}

			if (attribute != null)
			{
				Settings = attribute.Settings;
			}
		}

		/// <summary>
		/// Saves all changes made in this context to the database.
		/// </summary>
		/// <returns>
		/// The number of state entries written to the database.
		/// </returns>
		/// <remarks>
		/// This method will automatically call <see cref="M:Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.DetectChanges" /> to discover any
		/// changes to entity instances before saving to the underlying database. This can be disabled via
		/// <see cref="P:Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AutoDetectChangesEnabled" />.
		/// </remarks>
		public override int SaveChanges()
		{
			return SaveChanges(() => base.SaveChanges());
		}
		
		/// <summary>
		/// Asynchronously saves all changes made in this context to the database.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the task to complete.</param>
		/// <returns>
		/// A task that represents the asynchronous save operation. The task result contains the
		/// number of state entries written to the database.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method will automatically call <see cref="M:Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.DetectChanges" /> to discover any
		/// changes to entity instances before saving to the underlying database. This can be disabled via
		/// <see cref="P:Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AutoDetectChangesEnabled" />.
		/// </para>
		/// <para>
		/// Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
		/// that any asynchronous operations have completed before calling another method on this context.
		/// </para>
		/// </remarks>
		public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return await base.SaveChangesAsync(cancellationToken);
		}

		public List<EntityFrameworkAuditEvent> GetAllEvents()
		{
			return ArgusOptions.StorageProvider.GetEvents<EntityFrameworkAuditEvent>().ToList();
		}

		private int SaveChanges(Func<int> saveChanges)
		{
			if (AuditDisabled)
				return saveChanges();

			EntityFrameworkAuditEvent auditEvent = CreateAuditEvent();
			if (auditEvent == null)
				return saveChanges();

			int result = saveChanges(); // if the save changes throws an exception (fails), it will exit this function and we will not save the event

			foreach (DatabaseActionEntry auditEventEntry in auditEvent.DatabaseActions) // loop back over inserts to get new primary key value for database generated values
			{
				if (auditEventEntry.DatabaseAction == DatabaseAction.Insert)
				{
					object entity = auditEventEntry.EntityEntry.Entity;
					IEntityType entityType = Model.FindEntityType(entity.GetType()); // TODO: cache
					auditEventEntry.PrimaryKey = entityType.GetPrimaryKeyValue(entity);
					foreach (KeyValuePair<string, object> keyValuePair in auditEventEntry.PrimaryKey)
					{
						auditEventEntry.ColumnValues[keyValuePair.Key].OriginalValue = keyValuePair.Value;
					}
				}
			}

			ArgusOptions.StorageProvider.InsertEvent(auditEvent);

			return result;
		}

		private EntityFrameworkAuditEvent CreateAuditEvent()
		{
			List<EntityEntry> modifiedEntries = GetModifiedEntries();
			if (modifiedEntries.Count == 0)
				return null;

			DbConnection connection = this.IsProviderRelational() ? Database.GetDbConnection() : null;
			string connectionID = connection?.GetConnectionID();

			EntityFrameworkAuditEvent auditEvent = new EntityFrameworkAuditEvent()
			{
				DatabaseActions = new List<DatabaseActionEntry>(),
			};

			if (Settings.IncludeDatabase)
				auditEvent.Database = connection?.Database;
			if (Settings.IncludeConnectionID)
				auditEvent.ConnectionID = connectionID;
			if (Settings.IncludeTransactionID)
				auditEvent.TransactionID = this.GetCurrentTransactionID();

			foreach (EntityEntry modifiedEntry in modifiedEntries)
			{
				object entity = modifiedEntry.Entity;
				IEntityType entityType = Model.FindEntityType(entity.GetType()); // TODO: cache

				Dictionary<string, FieldValueChange> columnValues = modifiedEntry.State == EntityState.Modified ? GetChanges(modifiedEntry) : GetColumnValues(modifiedEntry);

				if (columnValues.Count == 0)
					continue;

				DatabaseActionEntry entry = new DatabaseActionEntry()
				{
					EntityEntry = modifiedEntry,
					DatabaseAction = EntityFrameworkHelpers.GetActionFromEntityState(modifiedEntry.State),
					ColumnValues = columnValues,
					PrimaryKey = entityType.GetPrimaryKeyValue(entity),
					Table = this.GetDbSetForType(entity.GetType()).Name // TODO: give option of table name
				};

				auditEvent.DatabaseActions.Add(entry);
			}

			return auditEvent.DatabaseActions.Count != 0 ? auditEvent : null;
		}

		private Dictionary<string, FieldValueChange> GetChanges(EntityEntry entry)
		{
			Dictionary<string, FieldValueChange> changes = new Dictionary<string, FieldValueChange>();
			Type type = entry.Entity.GetType();
			IEnumerable<IProperty> properties = null;
			if (!EntityPropertiesCache.TryGetValue(type, out properties))
			{
				properties = Model.FindEntityType(type).GetProperties();
				EntityPropertiesCache[type] = properties;
			}

			foreach (IProperty property in properties)
			{
				bool? ignore = GetIgnoreUpdateAttribute(property.PropertyInfo);

				if (ignore != null && ignore.Value == true)
					continue;

				PropertyEntry propertyEntry = entry.Property(property.Name);
				if (propertyEntry.IsModified)
				{
					// TODO: give option of property name or column name
					changes[property.GetColumnName()] = new FieldValueChange()
					{
						NewValue = propertyEntry.CurrentValue,
						OriginalValue = propertyEntry.OriginalValue
					};
				}
			}
			return changes;
		}

		private Dictionary<string, FieldValueChange> GetColumnValues(EntityEntry entry)
		{
			Dictionary<string, FieldValueChange> columnValues = new Dictionary<string, FieldValueChange>();
			Type type = entry.Entity.GetType();
			IEnumerable<IProperty> properties = null;
			if (!EntityPropertiesCache.TryGetValue(type, out properties))
			{
				properties = Model.FindEntityType(type).GetProperties();
				EntityPropertiesCache[type] = properties;
			}

			foreach (IProperty property in properties)
			{
				PropertyEntry propEntry = entry.Property(property.Name);
				object value = (entry.State != EntityState.Deleted) ? propEntry.CurrentValue : propEntry.OriginalValue;
				columnValues[property.Name] = new FieldValueChange()
				{
					OriginalValue = value
				};
			}
			return columnValues;
		}

		private List<EntityEntry> GetModifiedEntries()
		{
			return ChangeTracker.Entries()
								.Where(x => x.State != EntityState.Unchanged &&
											x.State != EntityState.Detached &&
											ShouldIncludeEntity(x))
								.ToList();
		}

		private bool ShouldIncludeEntity(EntityEntry entry)
		{
			Type type = entry.Entity.GetType();

			bool? include = GetIncludeIgnoreAttribute(type);
			if (Settings.MemberMode == MemberMode.OptIn && (include == null || include.Value == false))
				return false;
			else if (include != null && include.Value == false)
				return false;

			DatabaseAction? includeDatabaseActions = GetActionsAttribute(type);
			DatabaseAction databaseAction = EntityFrameworkHelpers.GetActionFromEntityState(entry.State);
			return includeDatabaseActions == null || includeDatabaseActions.Value.HasFlag(databaseAction);
		}

		private bool? GetIncludeIgnoreAttribute(Type type)
		{
			if (IncludeIgnoreAttributeCache.ContainsKey(type))
				return IncludeIgnoreAttributeCache[type];

			PropertyInfo dbSet = this.GetDbSetForType(type);

			if (dbSet != null)
			{
				if (dbSet.GetCustomAttribute<AuditIncludeAttribute>() != null)
				{
					IncludeIgnoreAttributeCache[type] = true;
					return true;
				}
				else if (dbSet.GetCustomAttribute<AuditIgnoreAttribute>() != null)
				{
					IncludeIgnoreAttributeCache[type] = false;
					return false;
				}
				else
				{
					IncludeIgnoreAttributeCache[type] = null;
					return null;
				}
			}

			TypeInfo typeInfo = type.GetTypeInfo();
			if (typeInfo.GetCustomAttribute<AuditIncludeAttribute>() != null)
			{
				IncludeIgnoreAttributeCache[type] = true;
				return true;
			}
			else if (typeInfo.GetCustomAttribute<AuditIgnoreAttribute>() != null)
			{
				IncludeIgnoreAttributeCache[type] = false;
				return false;
			}
			else
			{
				IncludeIgnoreAttributeCache[type] = null;
				return null;
			}
		}

		private DatabaseAction? GetActionsAttribute(Type type)
		{
			if (ActionsAttributeCache.ContainsKey(type))
				return ActionsAttributeCache[type];

			PropertyInfo dbSet = this.GetDbSetForType(type);

			if (dbSet != null)
			{
				AuditDatabaseActionsAttribute attribute = dbSet.GetCustomAttribute<AuditDatabaseActionsAttribute>();
				if (attribute != null)
				{
					ActionsAttributeCache[type] = attribute.TrackActions;
					return attribute.TrackActions;
				}
			}

			TypeInfo typeInfo = GetType().GetTypeInfo();
			AuditDbContextAttribute dbContextAttribute = typeInfo.GetCustomAttribute<AuditDbContextAttribute>();
			if (dbContextAttribute != null)
			{
				ActionsAttributeCache[type] = dbContextAttribute.TrackActions;
				return dbContextAttribute.TrackActions;
			}
			else
			{
				ActionsAttributeCache[type] = null;
				return null;
			}
		}

		private bool? GetIgnoreUpdateAttribute(PropertyInfo propertyInfo)
		{
			if (IgnoreUpdateAttributeCache.ContainsKey(propertyInfo))
				return IgnoreUpdateAttributeCache[propertyInfo];

			if (propertyInfo.GetCustomAttribute<AuditIncludeAttribute>() != null)
			{
				IgnoreUpdateAttributeCache[propertyInfo] = true;
				return true;
			}
			else if (propertyInfo.GetCustomAttribute<AuditIgnoreAttribute>() != null)
			{
				IgnoreUpdateAttributeCache[propertyInfo] = false;
				return false;
			}
			else
			{
				IgnoreUpdateAttributeCache[propertyInfo] = null;
				return null;
			}
		}
	}
}
