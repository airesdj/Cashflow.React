using System.Text;

namespace CashFlow.Core.Classes
{
    public class StringUtils : IDisposable
    {
        private static readonly object locker = new object();
        private StringBuilder CommonString = null!;

        public StringUtils()
        {
            Initialize();
        }


        private void Initialize()
        {
            if (CommonString == null)
                CommonString = new StringBuilder();
        }

        public StringUtils Append(string text)
        {
            Initialize();
            CommonString.Append(text);
            return this;
        }

        public StringUtils Append(int text)
        {
            Initialize();
            CommonString.Append(text);
            return this;
        }

        public StringUtils Append(long text)
        {
            Initialize();
            CommonString.Append(text);
            return this;
        }

        public StringUtils AppendTextWithSingleQuote(string text)
        {
            Initialize();
            CommonString.Append("'");
            CommonString.Append(text);
            CommonString.Append("'");
            return this;
        }

        public StringUtils AppendTextWithQuotes(string text)
        {
            Initialize();
            CommonString.Append("\"");
            CommonString.Append(text);
            CommonString.Append("\"");
            return this;
        }

        public StringUtils AppendTextWithSquareBrackets(string text)
        {
            Initialize();
            CommonString.Append("[");
            CommonString.Append(text);
            CommonString.Append("]");
            return this;
        }

        public StringUtils AppendTextWithBrackets(string text)
        {
            Initialize();
            CommonString.Append("(");
            CommonString.Append(text);
            CommonString.Append(")");
            return this;
        }

        public StringUtils AppendTextWithBracketsAndSignleQuotes(string text)
        {
            Initialize();
            CommonString.Append("(");
            CommonString.Append("'");
            CommonString.Append(text);
            CommonString.Append("'");
            CommonString.Append(")");
            return this;
        }

        public StringUtils AppendTextWithFullStop(string text)
        {
            Initialize();
            CommonString.Append(text);
            CommonString.Append(".");
            return this;
        }

        public StringUtils AppendSpace()
        {
            Initialize();
            CommonString.Append(" ");
            return this;
        }

        public StringUtils AppendSpace(int totalWidth)
        {
            Initialize();
            CommonString.Append(string.Concat(Enumerable.Repeat(" ", totalWidth)));
            return this;
        }

        public StringUtils AppendDash()
        {
            Initialize();
            CommonString.Append("-");
            return this;
        }

        public StringUtils AppendDashWithSpaces()
        {
            Initialize();
            CommonString.Append(" - ");
            return this;
        }

        public StringUtils AppendComma()
        {
            Initialize();
            CommonString.Append(",");
            return this;
        }

        public StringUtils AppendSemicolon()
        {
            Initialize();
            CommonString.Append(";");
            return this;
        }

        public StringUtils AppendSlash()
        {
            Initialize();
            CommonString.Append("/");
            return this;
        }

        public StringUtils AppendBackSlash()
        {
            Initialize();
            CommonString.Append("\\");
            return this;
        }

        public StringUtils AppendDot()
        {
            Initialize();
            CommonString.Append(".");
            return this;
        }

        public StringUtils AppendLine(string text)
        {
            Initialize();
            CommonString.AppendLine(text);
            return this;
        }

        public StringUtils AppendLine()
        {
            Initialize();
            CommonString.AppendLine();
            return this;
        }

        public bool HasValue()
        {
            Initialize();
            return CommonString.ToString().Length > 0;
        }

        public override string ToString()
        {
            string ret = string.Empty;
            try
            {
                Initialize();
                lock (locker)
                {
                    ret = CommonString.ToString();
                }
                Dispose();
            }
            catch { }
            return ret;
        }

        public void Dispose()
        {
            if (CommonString != null)
            {
                try { CommonString.Clear(); } catch { }
                CommonString = null!;
            }
        }
    }
}