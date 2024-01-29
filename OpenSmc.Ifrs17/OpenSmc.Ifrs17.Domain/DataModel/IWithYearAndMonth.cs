namespace OpenSmc.Ifrs17.Domain.DataModel
{
    public interface IWithYearAndMonth
    {
        public int Year { get; init; }

        public int Month { get; init; }
    }

}
