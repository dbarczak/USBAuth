namespace Model
{
    public class Device
    {
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public string OwnerName { get; set; }
        public string PublicKeyPem { get; set; }
        public byte[] PinHash { get; set; }
        public byte[] PinSalt { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
