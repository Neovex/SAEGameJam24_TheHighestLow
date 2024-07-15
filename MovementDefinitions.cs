namespace LowHigh
{
    public class Walk
    {
        public float Acceleration { get; set; } = 5;
        public float MaxSpeed { get; set; } = 600;
        public float Deceleration { get; set; } = 70;
        public float TurnSpeed { get; set; } = 8;
    }

    public class Jump
    {
        public float DefaultGravity { get; set; } = 30;
        public float ApexGravity { get; set; } = 100;
        public float JumpForce { get; set; } = -1000;
        public float JumpCutoffTime { get; set; } = 0.1f;
        public float TerminalVelocity { get; set; } = 1500;
    }
}