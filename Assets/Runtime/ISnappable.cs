using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplineEditor
{
    public interface ISnappable
    {
        public void GridSnap();
        public void LineSnap();
    }
}
