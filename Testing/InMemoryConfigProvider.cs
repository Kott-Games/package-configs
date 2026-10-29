using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kott.Configs.Testing
{
    /// <summary>
    ///     Config provider for unit tests: templates created in code, same lookups as a loaded
    ///     <see cref="ConfigProvider" />. With Scopes, use <c>InMemoryConfigManager</c> to inject it as <c>IConfigManager</c>.
    ///     <see cref="Dispose" /> destroys the templates it created.
    /// </summary>
    /// <example>
    ///     <code>
    ///     using InMemoryConfigProvider configs = new();
    ///     configs.Add&lt;ItemTemplate&gt;("Item+Sword", item => item.Damage = 5);
    ///     </code>
    /// </example>
    public class InMemoryConfigProvider : IConfigProvider, IDisposable
    {
        private readonly List<Template> _templates = new();
        private readonly List<Template> _created = new();
        private readonly List<Type> _knownTypes = new();
        private TemplateCatalog _catalog;

        /// <summary>Creates a template of type <typeparamref name="T" /> with <paramref name="id" /> and adds it.</summary>
        /// <param name="setup">Sets the template's data.</param>
        public T Add<T>(string id, Action<T> setup = null) where T : Template
        {
            T template = ScriptableObject.CreateInstance<T>();
            template.name = id;
            template.ID = id;
            setup?.Invoke(template);
            _created.Add(template);
            return Add(template);
        }

        /// <summary>Adds an existing template (not destroyed by <see cref="Dispose" />).</summary>
        public T Add<T>(T template) where T : Template
        {
            if (!template) throw new ArgumentNullException(nameof(template));
            _templates.Add(template);
            _catalog = null;
            return template;
        }

        /// <summary>Makes <typeparamref name="T" /> known without templates: its collection is empty instead of missing.</summary>
        public InMemoryConfigProvider AddType<T>() where T : Template
        {
            _knownTypes.Add(typeof(T));
            _catalog = null;
            return this;
        }

        /// <inheritdoc />
        public T GetTemplate<T>(string id) where T : Template => Catalog.GetTemplate<T>(id);

        /// <inheritdoc />
        public bool TryGetTemplate<T>(string id, out T template) where T : Template =>
            Catalog.TryGetTemplate(id, out template);

        /// <inheritdoc />
        public T GetTemplate<T>(Enum value) where T : Template => Catalog.GetTemplate<T>(value);

        /// <inheritdoc />
        public IReadOnlyList<T> GetCollection<T>() where T : Template => Catalog.GetCollection<T>();

        /// <summary>Destroys the templates created by <see cref="Add{T}(string, Action{T})" />.</summary>
        public void Dispose()
        {
            foreach (Template template in _created)
            {
                ConfigObjectUtility.Destroy(template);
            }

            _created.Clear();
            _templates.Clear();
            _catalog = null;
        }

        private TemplateCatalog Catalog => _catalog ??= new TemplateCatalog(_templates, _knownTypes);
    }
}
