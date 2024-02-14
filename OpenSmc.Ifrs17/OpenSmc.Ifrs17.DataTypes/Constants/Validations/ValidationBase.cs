namespace OpenSmc.Ifrs17.DataTypes.Constants.Validations;

public abstract class ValidationBase
{
    public string MessageCode { get; protected set; }

    protected ValidationBase(string messageCode)
    {
        MessageCode = messageCode;
    }

    public abstract string GetMessage(params string[] s);
}