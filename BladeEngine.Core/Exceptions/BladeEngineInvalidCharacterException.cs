using System;

namespace BladeEngine.Core.Exceptions
{
    [Serializable]
    public class BladeEngineInvalidCharacterException : BladeEngineException
    {
        public char Char { get; private set; }
        public int Row { get; private set; }

        public int Col { get; private set; }
        public int Position { get; private set; }
        public string Expected { get; private set; }
        public string Type { get; private set; }
        public string State { get; private set; }
        public BladeEngineInvalidCharacterException(char ch, int position, int row, int col, string type, string state, string expected):
            base($"BladeEngine Parse Error '{type}' at position {position} ({row}, {col}). Expected {expected}, but saw '{ch}' (state = {state})")
        {
            Init(ch, position, row, col, type, state, expected);
        }
        public BladeEngineInvalidCharacterException(char ch, int position, int row, int col, string state, string expected) :
            base($"BladeEngine Parse Error at position {position} ({row}, {col}). Expected {expected}, but saw '{ch}' (state = {state})")
        {
            Init(ch, position, row, col, "", state, expected);
        }
        private void Init(char ch, int position, int row, int col, string type, string state, string expected)
        {
            this.Char = ch;
            this.Expected = expected;
            this.Type = type;
            this.State = state;
            this.Position = position;
            this.Col = col;
            this.Row = row;
        }
    }
}