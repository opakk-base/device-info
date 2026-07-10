namespace GetDevice.Services;

public interface IPasswordService
{
    bool Verify(string password);
    bool Change(string currentPassword, string newPassword);
    bool IsDefaultPassword();
    void ResetToDefault();
}
