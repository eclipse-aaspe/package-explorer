using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxPackageLogic
{
    public interface IExecuteMainCommand
    {
        Task<int> ExecuteMainMenuCommand(string menuItemName, bool scriptMode, params object[] args);
    }
}
