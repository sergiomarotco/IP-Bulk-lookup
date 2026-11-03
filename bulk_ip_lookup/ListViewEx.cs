namespace IP_Bulk_Lookup
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    /// <summary>
    /// ListView, который уведомляет (<see cref="ColumnResizeEnded"/>), что пользователь закончил
    /// менять ширину колонки (перетаскивание границы, двойной клик по границе или автоподгонка).
    /// Нужен, чтобы последняя колонка всегда дозаполняла ширину таблицы - без "пустой бесконечной колонки" справа.
    /// </summary>
    public class ListViewEx : ListView
    {
        /// <summary>
        /// Сообщение от дочернего окна (header control) родителю.
        /// </summary>
        private const int WmNotify = 0x004E;

        /// <summary>
        /// HDN_FIRST = (0-300). Отрицательные коды уведомлений header control.
        /// </summary>
        private const int HdnDividerDblClick = -300 - 5; // двойной клик по границе колонок

        private const int HdnEndTrack = -300 - 7; // завершение перетаскивания границы колонки

        private const int HdnEndDrag = -300 - 11; // завершение операции перетаскивания

        /// <summary>
        /// Возникает после изменения ширины колонки пользователем.
        /// </summary>
        public event EventHandler ColumnResizeEnded;

        /// <inheritdoc/>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WmNotify && m.LParam != IntPtr.Zero)
            {
                Nmhdr header = (Nmhdr)Marshal.PtrToStructure(m.LParam, typeof(Nmhdr));

                // Сравниваем с беззнаковым представлением отрицательных HDN_* кодов
                if (header.Code == unchecked((uint)HdnDividerDblClick) ||
                    header.Code == unchecked((uint)HdnEndTrack) ||
                    header.Code == unchecked((uint)HdnEndDrag))
                {
                    // Откладываем на очередь сообщений, чтобы новая ширина колонки уже применилась
                    this.BeginInvoke((Action)(() => this.ColumnResizeEnded?.Invoke(this, EventArgs.Empty)));
                }
            }
        }

        /// <summary>
        /// Структура NMHDR из Win32 (заголовок notification-сообщения).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct Nmhdr
        {
            /// <summary>Дескриптор окна, отправившего уведомление.</summary>
            public IntPtr HwndFrom;

            /// <summary>Идентификатор контрола.</summary>
            public IntPtr IdFrom;

            /// <summary>Код уведомления (HDN_*).</summary>
            public uint Code;
        }
    }
}
