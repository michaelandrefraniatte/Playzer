namespace Valuechanges
{
    public class Valuechange
    {
        public static double[] _valuechange = { 0, 0, 0, 0 };
        public static double[] _ValueChange = { 0, 0, 0, 0 };
        public double this[int index]
        {
            get { return _ValueChange[index]; }
            set
            {
                if (_valuechange[index] != value)
                    _ValueChange[index] = value - _valuechange[index];
                else
                    _ValueChange[index] = 0;
                _valuechange[index] = value;
            }
        }
    }
}