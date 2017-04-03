using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Argus.MongoDB
{
	internal class FieldValueChangeSerializer : ObjectSerializer, IBsonSerializer<FieldValueChange>
	{
		public FieldValueChange Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			FieldValueChange fieldValueChange = new FieldValueChange();
			IBsonReader reader = context.Reader;

			BsonType bysonType = reader.GetCurrentBsonType();
			if (bysonType == BsonType.Document)
			{
				reader.ReadStartDocument();
				string name = reader.ReadName();
				fieldValueChange.OriginalValue = base.Deserialize(context, args);
				name = reader.ReadName();
				fieldValueChange.NewValue = base.Deserialize(context, args);
				reader.ReadEndDocument();

				return fieldValueChange;
			}
			else
			{
				fieldValueChange.OriginalValue = base.Deserialize(context, args);
			}

			return fieldValueChange;
		}

		public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, FieldValueChange value)
		{
			if (value.OriginalValue == null || value.NewValue == null)
			{
				object objectValue = value.OriginalValue ?? value.NewValue;
				//if (objectValue != null)
				//{
				//	IBsonSerializer serializer = BsonSerializer.LookupSerializer(objectValue.GetType());
				//	serializer.Serialize(context, args, objectValue);
				//}
				
				base.Serialize(context, args, objectValue);
			}
			else
			{
				IBsonWriter writer = context.Writer;

				writer.WriteStartDocument();

				writer.WriteName(nameof(value.OriginalValue));
				IBsonSerializer serializer = BsonSerializer.LookupSerializer(value.OriginalValue.GetType());
				serializer.Serialize(context, args, value.OriginalValue);

				writer.WriteName(nameof(value.NewValue));
				serializer = BsonSerializer.LookupSerializer(value.NewValue.GetType());
				serializer.Serialize(context, args, value.NewValue);

				writer.WriteEndDocument();
			}
		}
	}
}
