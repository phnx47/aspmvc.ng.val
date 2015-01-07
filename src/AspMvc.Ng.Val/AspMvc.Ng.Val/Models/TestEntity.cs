using System.ComponentModel.DataAnnotations;

namespace AspMvc.Ng.Val.Models
{
    public class TestEntity
    {
        [Required]
        public string RequiredProperty { get; set; }

        [Range(0, 10)]
        public int Range { get; set; }

        [Required]
        [RegularExpression("\\d")]
        public string MultipleValidationProperty { get; set; }
    }
}