using System.Collections;
using System.Collections.Generic;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core.Utils
{
    public class CharReader : IEnumerable<char>
    {
        public CharReader(string source)
        {
            this.source = source;

            Reset();
        }
        #region Fields & Props
        private char? lastChar;
        private string source;
        public string Source
        {
            get { return source; }
            set
            {
                source = value;
                Reset();
            }
        }
        private int position;
        public int Position
        {
            get { return position + 1; }
        }
        private int col;
        public int Col
        {
            get { return col; }
        }
        private int row;
        public int Row
        {
            get { return row; }
        }
        private char currentChar;
        public char Current
        {
            get
            {
                return currentChar;
            }
        }
        #endregion
        #region Methods
        public bool Next()
        {
            var result = false;

            currentChar = default;

            if (lastChar.HasValue)
            {
                currentChar = lastChar.Value;
                lastChar = null;

                result = true;
            }
            else
            {
                if (source.Length > 0 && position < source.Length)
                {
                    position++;

                    if (position < source.Length)
                    {
                        currentChar = source[position];

                        if (currentChar == '\n')
                        {
                            row++;
                            col = 0;
                        }
                        else
                        {
                            col++;
                        }

                        result = true;
                    }
                    else
                    {
                        col++;
                    }
                }
            }

            return result;
        }
        public void Reset()
        {
            position = -1;
            col = 0;
            row = 1;
            currentChar = default;

            if (source == null)
            {
                source = "";
            }
        }
        public void Store()
        {
            if (position >= 0 && position < source.Length)
            {
                lastChar = currentChar;
            }
        }
        public bool HasEndedWith(string value)
        {
            var result = true;

            if (IsSomeString(value))
            {
                for (var i = 0; i < value.Length; i++)
                {
                    if (value[i] != source[position - value.Length + i + 1])
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }
        #endregion
        public IEnumerator<char> GetEnumerator()
        {
            while (Next())
            {
                yield return Current;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
