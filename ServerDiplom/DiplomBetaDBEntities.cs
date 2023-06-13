using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerDiplom
{
    public partial class DiplomBetaDBEntities : DbContext
    {
        private static DiplomBetaDBEntities _context;
        public static DiplomBetaDBEntities GetContext()
        {
            if (_context == null)
                _context = new DiplomBetaDBEntities();
            return _context;
        }
    }
}
