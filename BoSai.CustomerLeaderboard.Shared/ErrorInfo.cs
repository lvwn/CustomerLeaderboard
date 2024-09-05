using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoSai.CustomerLeaderboard.Shared
{
    public class ErrorInfo
    {
        public string ErrorCode { get; set; }

        public string ErrorMsg { get; set; }
        public ErrorInfo(string errorCode, string errorMsg)
        {
            ErrorCode = errorCode;
            ErrorMsg = errorMsg;
        }
    }
}
