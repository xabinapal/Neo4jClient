using System;

namespace Neo4jClient
{
    public class NeoServerException : ApplicationException
    {
        readonly string code;
        readonly string status;

        public NeoServerException(string code, string status, string message)
            : base(message)
        {
            this.code = code;
            this.status = status;
        }

        public string Code { get { return code; } }

        public string Status { get { return status; } }

        public override string Message
        {
            get { return string.Format("{0} {1} {2}", Code, Status, base.Message); }
        }
    }
}
