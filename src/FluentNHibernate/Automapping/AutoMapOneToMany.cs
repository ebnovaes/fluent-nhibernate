using System;
using System.Collections.Generic;
using System.Reflection;
using FluentNHibernate.Mapping;
using FluentNHibernate.MappingModel;
using FluentNHibernate.MappingModel.ClassBased;
using FluentNHibernate.MappingModel.Collections;
using FluentNHibernate.Utils;

namespace FluentNHibernate.Automapping
{
    public class AutoMapOneToMany : IAutoMapper
    {
        private readonly AutoMappingExpressions expressions;
        private readonly Func<Member, bool> findPropertyconvention = 
            p => (p.PropertyType.Namespace == "System.Collections.Generic" ||
                  p.PropertyType.Namespace == "Iesi.Collections.Generic");

        public AutoMapOneToMany(AutoMappingExpressions expressions)
        {
            this.expressions = expressions;
        }

        public bool MapsProperty(Member property)
        {
            if (property.CanWrite)
                return findPropertyconvention(property);

            return false;
        }

        public void Map(ClassMappingBase classMap, Member property)
        {
            if (property.DeclaringType != classMap.Type)
                return;

            var mapping = GetCollectionMapping(property.PropertyType);

            mapping.ContainingEntityType = classMap.Type;
            mapping.Member = property;
            mapping.SetDefaultValue(x => x.Name, property.Name);

            SetRelationship(property, classMap, mapping);
            SetKey(property, classMap, mapping);

            classMap.AddCollection(mapping);        
        }

        private ICollectionMapping GetCollectionMapping(Type type)
        {
            if (type.Namespace.StartsWith("Iesi.Collections") || type.Closes(typeof(HashSet<>)))
                return new SetMapping();

            return new BagMapping();
        }

        private void SetRelationship(Member property, ClassMappingBase classMap, ICollectionMapping mapping)
        {
            var relationship = new OneToManyMapping
            {
                Class = new TypeReference(property.PropertyType.GetGenericArguments()[0]),
                ContainingEntityType = classMap.Type
            };

            mapping.SetDefaultValue(x => x.Relationship, relationship);
        }

        private void SetKey(Member property, ClassMappingBase classMap, ICollectionMapping mapping)
        {
            var columnName = property.DeclaringType.Name + "_Id";

            if (classMap is ComponentMapping)
                columnName = expressions.GetComponentColumnPrefix(((ComponentMapping)classMap).Member) + columnName;

            var key = new KeyMapping();

            key.ContainingEntityType = classMap.Type;
            key.AddDefaultColumn(new ColumnMapping { Name = columnName});

            mapping.SetDefaultValue(x => x.Key, key);
        }
    }
}
