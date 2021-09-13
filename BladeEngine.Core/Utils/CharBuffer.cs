using System;
using static BladeEngine.Core.Utils.LanguageConstructs;

namespace BladeEngine.Core.Utils
{
    public class CharBuffer
    {
        const int DEFAULT_BUFFER_SIZE = 32;
        private int _bufferSize;
        private char[] _buffer;
        private int _position;
        public CharBuffer() : this(DEFAULT_BUFFER_SIZE, "")
        { }
        public CharBuffer(int bufferSize) : this(bufferSize, "")
        { }
        public CharBuffer(string init) : this(0, init)
        { }
        public CharBuffer(int bufferSize, string init)
        {
            BufferSize = bufferSize;

            _buffer = new char[BufferSize];

            Append(init);
        }
        public int Length
        {
            get { return _position; }
        }
        public int BufferSize
        {
            get
            {
                return _bufferSize;
            }
            set
            {
                if (value > 0)
                {
                    _bufferSize = value;
                }
                else
                {
                    if (_bufferSize == 0)
                    {
                        _bufferSize = DEFAULT_BUFFER_SIZE;
                    }
                }
            }
        }
        public int BufferLength
        {
            get { return _buffer.Length; }
        }

        public void Reset()
        {
            _position = 0;
        }
        public void Clear()
        {
            Reset();

            this._buffer = new char[BufferSize];
        }
        public void Append(char ch)
        {
            if (_position == _buffer.Length)
            {
                Array.Resize(ref _buffer, _buffer.Length + BufferSize);
            }

            _buffer[_position++] = ch;
        }
        public void Append(string s)
        {
            if (IsSomeString(s))
            {
                if (_buffer.Length - _position < s.Length)
                {
                    Array.Resize(ref _buffer, _buffer.Length + (s.Length - _buffer.Length + _position) + BufferSize / 2);
                }

                Array.Copy(s.ToCharArray(), 0, _buffer, _position, s.Length);

                _position += s.Length;
            }
        }
        public void AppendFormat(string format, params object[] args)
        {
            var s = string.Format(format, args);

            Append(s);
        }
        public void Append(params char[] arr)
        {
            if (arr != null && arr.Length > 0)
            {
                if (_buffer.Length - _position < arr.Length)
                {
                    Array.Resize(ref _buffer, _buffer.Length + BufferSize);
                }

                Array.Copy(arr, 0, _buffer, _position, arr.Length);

                _position += arr.Length;
            }
        }
        public override string ToString()
        {
            return new string(_buffer, 0, _position);
        }
        public string Flush()
        {
            var result = ToString();

            Reset();

            return result;
        }
        public char[] GetBuffer()
        {
            if (_position > 0)
            {
                var result = new char[_position];

                Array.Copy(_buffer, 0, result, 0, _position);

                return result;
            }
            else
            {
                return new char[] { };
            }
        }
        public char CharAt(int index)
        {
            if (index < 0 || index >= _position)
                throw new IndexOutOfRangeException();

            return _buffer[index];
        }
        public char LastCharAt(int index)
        {
            if (index < 0 || index >= _position)
                throw new IndexOutOfRangeException();

            return _buffer[_position - index - 1];
        }
        public char this[int index]
        {
            get
            {
                return CharAt(index);
            }
            set
            {
                if (index < 0 || index >= _position)
                {
                    throw new IndexOutOfRangeException();
                }

                _buffer[index] = value;
            }
        }
        public string Substring(int startIndex, int length)
        {
            if (startIndex < 0)
            {
                throw new ArgumentException($"Invalid start index {startIndex}");
            }

            if (length < 0)
            {
                throw new ArgumentException($"Invalid length {length}");
            }

            if (length == 0)
            {
                return "";
            }

            var _length = startIndex + length > _position ? _position - startIndex : length;

            return new string(_buffer, startIndex, _length);
        }
        public static CharBuffer operator +(CharBuffer buffer, char ch)
        {
            buffer.Append(ch);

            return buffer;
        }
        public static CharBuffer operator +(char ch, CharBuffer buffer)
        {
            buffer.Append(ch);

            return buffer;
        }
        public static CharBuffer operator +(CharBuffer buffer, string s)
        {
            buffer.Append(s);

            return buffer;
        }
        public static CharBuffer operator +(string s, CharBuffer buffer)
        {
            buffer.Append(s);

            return buffer;
        }
        public static CharBuffer operator +(CharBuffer buf1, CharBuffer buf2)
        {
            var buf = buf2.GetBuffer();

            buf1.Append(buf);

            return buf1;
        }
    }
}
