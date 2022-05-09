using Jekbot.Models;
using Jekbot.Utility;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Resources
{
    public class Database : PreparableResource<Database, Database.Factory>, Disposable.Contract
    {
        private Database(string connectionStr)
        {
            DB = new LiteDatabase(connectionStr);
        }

        public IEnumerable<ActionTimer> GetPassedTimers()
        {
            var col = DB.GetCollection<ActionTimer>();

            var list = col.Query()
                .Where(x => !x.Processed && x.ExpirationUtc <= DateTime.UtcNow)
                .ToList();

            foreach (var timer in list)
            {
                yield return timer;

                timer.Processed = true;
                col.Update(timer);
            }
        }

        private readonly LiteDatabase DB;

#region disposable
        bool Disposable.Contract.Disposed { get; set; }

        void Disposable.Contract.DisposeOnce()
        {
            //if (_database != null)
            //    _database.Dispose();
            //_database = null;
        }
#endregion

        public class Factory : IFactory<Database>
        {
            Database IFactory<Database>.Create(ResourceEnvironment environment) => new Database(environment.GetPath(DatabaseName));
            private const string DatabaseName = "jekbot.db";
        }
    }
}
