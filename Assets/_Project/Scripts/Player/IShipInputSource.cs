namespace Homecoming.Player
{
    /// <summary>기체 입력 공급원. 키보드·게임패드 입력과 봇이 모두 이 인터페이스를 구현한다.</summary>
    public interface IShipInputSource
    {
        ShipInputFrame ReadInput();
    }
}
