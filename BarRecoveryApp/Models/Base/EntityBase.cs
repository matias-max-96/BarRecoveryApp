using SQLite;

namespace BarRecoveryApp.Models.Base
{
    public abstract class EntityBase
    {
        [PrimaryKey]
        [MaxLength(36)]

        public string Id { get; set; } = Guid.NewGuid().ToString();              //Genera un identificador unico random de 128 bit y lo convierte en un char de 36 de largo
        public DateTime CreatedAtUtc { get; set; } = DateTime.Now;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
