using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Jekbot.Utility
{
    public static class LiteDbUtility
    {
        public static T FirstOrDefault<T>(this ILiteQueryable<T> self, Expression<Func<T, bool>> predicate)
        {
            return self.Where(predicate).FirstOrDefault();
        }
    }
}
