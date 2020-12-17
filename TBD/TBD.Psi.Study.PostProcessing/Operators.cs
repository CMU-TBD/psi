using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBD.Psi.Study.PostProcessing
{
    using Microsoft.Psi;
    public static class Operators
    {
        public static IProducer<List<T>> All<T>(this IProducer<T> input)
        {
            return input.Aggregate(new List<T>(), (acc, x) => { acc.Add(x); return acc; });
        }
    }
}
