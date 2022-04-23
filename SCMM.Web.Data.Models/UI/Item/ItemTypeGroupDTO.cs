namespace SCMM.Web.Data.Models.UI.Item
{
    public class ItemTypeGroupDTO
    {
        public string Name { get; set; }

        public IEnumerable<ItemTypeDTO> ItemTypes { get; set; }
    }
}
