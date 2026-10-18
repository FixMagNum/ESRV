namespace Game;

public interface IWaterloggable
{
	bool IsWaterLogged(int data);

	int SetWaterLogged(int data, bool isWaterLogged);
}
