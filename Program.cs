using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ecs
{
    #region interfaces
    public interface IEntity
    {
        ushort ID { get; }

        IEnumerable<IComponent> Comps { get; }
    }

    public interface IComponent
    { }

    public interface ISystem
    {
        IFilter Filter { get; }
    }

    public interface IRunSystem : ISystem
    {
        void Execute();
    }

    public interface IInitSystem : ISystem
    {
        void Init();
    }

    public interface IFilter
    {
        IEnumerable Entities { get; }

        void OnCompsChanged(IEntity e);
    }
    #endregion

    #region  filters
    abstract class Filter : IFilter
    {
        List<IEntity> es = new List<IEntity>();

        public IEnumerable Entities
        {
            get => es;
        }

        public Filter()
        {
            foreach (var e in GameState.GetEntities())
                OnCompsChanged(e);
        }

        public void OnCompsChanged(IEntity e)
        {
            if (IsValid(e))
                es.Add(e);
            else
                es.Remove(e);
        }

        protected abstract bool IsValid(IEntity e);
    }

    class Filter<T> : Filter where T : IComponent
    {
        protected override bool IsValid(IEntity e)
        {
            if(e.Comps.Any(x => x is T))
                return true;
            return false;
        }
    }

    class Filter<T, U> : Filter where T : IComponent where U : IComponent
    {
        protected override bool IsValid(IEntity e)
        {
            if(e.Comps.Any(x => x is T) && e.Comps.Any(x => x is U))
                return true;
            return false;
        }
    }
    #endregion

    #region managers
    public class SystemManager
    {
        List<ISystem> systems = new List<ISystem>();

        public void Init()
        {
            systems.Add(new SpawnSystem());
            systems.Add(new CheckSystem());

            foreach (IInitSystem s in systems.FindAll(x => x is IInitSystem))
                s.Init();
        }

        public void Update()
        {
            foreach (IRunSystem s in systems.FindAll(x => x is IRunSystem))
                s.Execute();
        }
    }

    public class EntityManager
    {
        List<IEntity> entities = new List<IEntity>();
        ushort lastID;

        public IEntity CreateEntity()
        {
            IEntity e = new Entity(GetNextID());
            entities.Add(e);
            return e;
        }

        public IEntity GetEntity(ushort id)
        {
            return entities.First(x => x.ID == id);
        }

        public IEnumerable<IEntity> GetEntities()
        {
            return entities;
        }

        ushort GetNextID()
        {
            return lastID += 1;
        }
    }

    public class ComponentManager
    {
        Dictionary<Type, List<IComponent>> components = new Dictionary<Type, List<IComponent>>();
        Dictionary<ushort, Dictionary<Type, IComponent>> entityComponents = new Dictionary<ushort, Dictionary<Type, IComponent>>();

        public IEnumerable<IComponent> GetComponents(ushort id)
        {
            return entityComponents[id].Values;
        }

        public void AddEntity(ushort id)
        {
            entityComponents.Add(id, new Dictionary<Type, IComponent>());
        }

        public void RemoveEntity(ushort id)
        {
            entityComponents.Remove(id);
        }

        public void AddComponent<T>(ushort id) where T : IComponent, new()
        {
            entityComponents[id].Add(typeof(T), new T());
        }

        public void RemoveComponent<T>(ushort id) where T : IComponent
        {
            entityComponents[id].Remove(typeof(T));
        }
    }

    public class FilterManager
    {
        Dictionary<Type, IFilter> filters = new Dictionary<Type, IFilter>();

        public IFilter GetFilter<T>() where T : IFilter, new()
        {
            if (!filters.ContainsKey(typeof(T)))
                filters.Add(typeof(T), new T());

            return filters[typeof(T)];
        }

        public void OnCompsChanged(IEntity e)
        {
            foreach (var f in filters.Values)
                f.OnCompsChanged(e);
        }
    }
    #endregion

    class Entity : IEntity
    {
        ushort id;

        public Entity(ushort id)
        {
            this.id = id;
        }

        public ushort ID { get => id; }

        public IEnumerable<IComponent> Comps { get => GameState.GetComponents(id); }
    }

    class Comp : IComponent
    { }

    class Comp2 : IComponent
    { }

    class SpawnSystem : IInitSystem
    {
        public IFilter Filter => null;

        public void Init()
        {
            for (int i = 0; i < 5; i++)
            {
                var e = GameState.CreateEntity();
                GameState.AddComponent<Comp>(e);
                GameState.AddComponent<Comp2>(e);
            }
        }
    }

    class CheckSystem : IRunSystem
    {
        public IFilter Filter { get => GameState.GetFilter<Filter<Comp, Comp2>>(); }

        public void Execute()
        {
            foreach (var e in Filter.Entities)
                Console.WriteLine(1);
        }
    }

    public static class GameState
    {
        static SystemManager systemManager = new SystemManager();
        static EntityManager entityManager = new EntityManager();
        static ComponentManager componentManager = new ComponentManager();
        static FilterManager filterManager = new FilterManager();

        public static void Start()
        {
            systemManager.Init();
            systemManager.Update();
        }

        public static IEntity CreateEntity()
        {
            var e = entityManager.CreateEntity();
            componentManager.AddEntity(e.ID);
            return e;
        }

        public static IEntity GetEntity(ushort id)
        {
            return entityManager.GetEntity(id);
        }

        public static IEnumerable<IEntity> GetEntities()
        {
            return entityManager.GetEntities();
        }

        public static IEnumerable<IComponent> GetComponents(ushort id)
        {
            return componentManager.GetComponents(id);
        }

        public static void AddComponent<T>(IEntity e) where T : IComponent, new()
        {
            componentManager.AddComponent<T>(e.ID);
            filterManager.OnCompsChanged(e);
        }

        public static void RemoveComponent<T>(IEntity e) where T : IComponent
        {
            componentManager.RemoveComponent<T>(e.ID);
            filterManager.OnCompsChanged(e);
        }

        public static IFilter GetFilter<T>() where T : IFilter, new()
        {
            return filterManager.GetFilter<T>();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            //Your code goes here
            GameState.Start();

        }
    }
}
