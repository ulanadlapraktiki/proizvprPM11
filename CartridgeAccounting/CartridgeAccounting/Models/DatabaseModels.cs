using System;

namespace CartridgeAccounting.Models
{
    /// <summary>
    /// Модель картриджа (таблица CartridgeModels)
    /// </summary>
    public class CartridgeModel
    {
        public int ModelID { get; set; }               // PK
        public string ModelName { get; set; }          // nvarchar(200), NOT NULL
        public string CompatiblePrinters { get; set; } // nvarchar(255), NULL
        public int? StandardResourse { get; set; }     // int, NULL (ресурс в страницах)
        public string ColorType { get; set; }          // nvarchar(20), NULL (черный/цветной)
    }

    /// <summary>
    /// Модель экземпляра картриджа (таблица Cartridges)
    /// </summary>
    public class Cartridge
    {
        public int CartridgesID { get; set; }          // PK
        public string InventoryNumber { get; set; }    // nvarchar(50), NOT NULL
        public int ModelID { get; set; }               // FK -> CartridgeModels
        public int CurrentStatusID { get; set; }       // FK -> Statuses
        public int? CurrentDepartmentID { get; set; }  // FK -> Departments, NULL
        public DateTime? PurchaseDate { get; set; }    // date, NULL
        public decimal? InitialCost { get; set; }      // decimal(10,2), NULL
        public bool? IsActive { get; set; }            // bit, NULL

        // Навигационные свойства (для удобства, не хранятся в БД)
        public CartridgeModel Model { get; set; }
        public Status CurrentStatus { get; set; }
        public Department CurrentDepartment { get; set; }
    }

    /// <summary>
    /// Справочник отделений (таблица Departments)
    /// </summary>
    public class Department
    {
        public int DepartmentsID { get; set; }         // PK
        public string DepartmentName { get; set; }     // nvarchar(150), NOT NULL
        public string Building { get; set; }           // nvarchar(50), NULL
        public string ResponsiblePerson { get; set; }  // nvarchar(100), NULL
    }

    /// <summary>
    /// Тип операции (таблица OperationsType)
    /// </summary>
    public class OperationsType
    {
        public int OperationsTypeID { get; set; }      // PK
        public string OperationsCode { get; set; }     // nvarchar(20), NOT NULL (Issue, Return, ToRefill, FromRefill)
        public string OperationName { get; set; }      // nvarchar(100), NOT NULL
        public string Direction { get; set; }          // nvarchar(20), NULL (In/Out)
    }

    /// <summary>
    /// Статус картриджа (таблица Statuses)
    /// </summary>
    public class Status
    {
        public int StatusID { get; set; }              // PK
        public string StatusName { get; set; }         // nvarchar(50), NULL
        public string StatusDiscription { get; set; }  // nvarchar(200), NULL
    }

    /// <summary>
    /// Журнал движения (таблица MovementHistory)
    /// </summary>
    public class MovementHistory
    {
        public int MovementID { get; set; }            
        public int CartridgeID { get; set; }           
        public DateTime MovementDateTime { get; set; } 
        public int OperationsTypeID { get; set; }      
        public int? DepartmentID { get; set; }        
        public string IssuedByEmployee { get; set; }   
        public string ReceivedByEmployee { get; set; } 
        public string Notes { get; set; }             

        // Навигационные свойства
        public Cartridge Cartridge { get; set; }
        public OperationsType OperationType { get; set; }
        public Department Department { get; set; }
    }
}