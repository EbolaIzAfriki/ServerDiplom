//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ServerDiplom
{
    using System;
    using System.Collections.Generic;
    
    public partial class Constraint
    {
        public int Id { get; set; }
        public int IdTask { get; set; }
        public int TypeConstraintId { get; set; }
        public Nullable<int> ProductCount { get; set; }
        public string IdPoints { get; set; }
    
        public virtual TypeConstraint TypeConstraint { get; set; }
        public virtual Task Task { get; set; }
    }
}
