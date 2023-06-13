using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerDiplom
{
    public class TCPMessege
    {
        /// <summary>
        /// Код операции: 1 = Get, 2 = GetBy, 3 = Create, 4 = Update, 5 = Delete; При ответе сервера 0 = неудача, 1 = удача<br/>
        /// Для объекта Point 1 - UpdateTaskPoints, 3 - CreatePointsList, 5 - DeletePointsList<br/>
        /// 41,8 - Обновление последнего состояния столбцов и строк тарифов матрицы
        /// </summary>
        public int CodeOperation;
        /// <summary>
        /// Коды сущности: 1 - User, 2 - Client, 3 - Carrier, 4 - Request, 35 - Service&Carrier, 5 - Point
        /// </summary>
        public int CodeEntity;
        public string Entity;
        /// <summary>
        /// Объект сообщения для передачи информации между клиентом и сервером
        /// </summary>
        /// <param name="O">Код операции: 1 = Get, 2 = GetBy, 3 = Create, 4 = Update, 5 = Delete; При ответе сервера 0 = неудача, 1 = удача <br/>Для объекта Point 1 - UpdateTaskPoints, 2 - CreateDeletePointsList </param>
        /// <param name="C">Коды сущности: 1 - User, 2 - Client, 3 - Carrier, 4 - Request, 35 - Service&Carrier, 5 - Point </param>
        /// <param name="E">Объект передачи</param>
        public TCPMessege(int O, int C, object E)
        {
            CodeOperation = O;
            CodeEntity = C;
            Entity = JsonConvert.SerializeObject(E, Formatting.Indented, new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
        }
    }
}
