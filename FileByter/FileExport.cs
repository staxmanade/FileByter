﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace FileByter
{
	public class FileExport
	{
		private readonly Type _rowType;
		protected readonly IDictionary<Type, IDictionary<Type, PropertyFormatter>> _defaultTypeFormatters = new Dictionary<Type, IDictionary<Type, PropertyFormatter>>();

		public FileExport(Type rowType)
		{
			_rowType = rowType;
			_defaultTypeFormatters.Add(_rowType, new Dictionary<Type, PropertyFormatter>());
		}
	}


	public class FileExport<T> : FileExport
	{
		public FileExport() : base(typeof(T))
		{
		}

		/// <summary>
		/// Create a <seealso cref="FileExportSpecification&lt;T&gt;" /> with the default configuration.
		/// </summary>
		/// <returns></returns>
		public FileExportSpecification<T> CreateSpec()
		{
			return CreateSpec(cfg => { });
		}

		/// <summary>
		/// Create a <seealso cref="FileExportSpecification&lt;T&gt;" /> by giving the option for custom configuration.
		/// </summary>
		public FileExportSpecification<T> CreateSpec(Action<FileExportSpecification<T>> configuration)
		{
			var fileExportSpecification = new FileExportSpecification<T>();
			configuration(fileExportSpecification);

			if (!fileExportSpecification.SkipNonConfiguredProperties)
			{
				ConfigureRestOfProperties(fileExportSpecification);
			}

			return fileExportSpecification;
		}

		private void ConfigureRestOfProperties(FileExportSpecification<T> fileExportSpecification)
		{
			// fallback formatter for anything that doesn't fit int he custom, or "global default" formatters.
			var globalDefaultFormatter = new PropertyFormatter(context =>
																{
																	if (context.ItemValue == null)
																	{
																		return string.Empty;
																	}
																	return context.ItemValue.ToString();
																});
			Type objectType = typeof(T);
			var properties = objectType.GetProperties();

			for (int i = 0; i < properties.Length; i++)
			{
				var propertyInfo = properties[i];
				var propertyName = propertyInfo.Name;

				if (!fileExportSpecification.IsPropertyExcluded(propertyName))
				{
					if (fileExportSpecification.IsPropertyDefined(propertyName))
					{
						fileExportSpecification[propertyName].Order = i;
						continue;
					}

					PropertyFormatter defaultPropertyFormatter = globalDefaultFormatter;

					// If there's a default
					if (_defaultTypeFormatters[objectType].ContainsKey(propertyInfo.PropertyType))
					{
						defaultPropertyFormatter = _defaultTypeFormatters[objectType][propertyInfo.PropertyType];
					}

					var property = new Property<T>(propertyName, defaultPropertyFormatter, fileExportSpecification.DefaultHeaderFormatter, i);

					fileExportSpecification.AddProperty(property);
				}
			}
		}

		public FileExport<T> AddDefault<TProperty>(Func<TProperty, string> formatter)
		{
			PropertyFormatter pf = context => formatter((TProperty)context.ItemValue);
			_defaultTypeFormatters[typeof(T)].Add(typeof(TProperty), pf);
			return this;
		}
	}

}