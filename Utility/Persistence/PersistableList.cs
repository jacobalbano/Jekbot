using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jekbot.Utility.Persistence
{
    public class PersistableList<T> : ObservableCollection<T>, IPersistable
        where T : new()
    {
        public string CollectionName { get; }

        public bool Dirty { get; private set; }

        public PersistableList(string collectionName)
        {
            CollectionName = collectionName;
            CollectionChanged += (_, _) => Dirty = true;
        }

        public void Persist(string toDirectory)
        {
            if (!Dirty) return;
            File.WriteAllText(
                MakePath(toDirectory),
                JsonSerializer.Serialize(this.ToArray())
            );
        }

        public void LoadPersistentData(string fromDirectory)
        {
            var path = MakePath(fromDirectory);
            if (!File.Exists(path)) return;

            foreach (var x in JsonSerializer.Deserialize<T[]>(File.ReadAllText(path))!)
                Add(x);

            Dirty = false;
        }

        private string MakePath(string toDirectory)
        {
            return $"{Path.Combine(toDirectory, CollectionName)}.json";
        }
    }
}
