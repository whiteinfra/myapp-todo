// MyApp — менеджер справ для Windows (Windows Forms, .NET Framework 4.x).
// Windows-версія застосунку To-Do з лабораторної роботи № 6. Сумісно з C# 5 (csc.exe з .NET Framework 4).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("MyApp — менеджер справ")]
[assembly: AssemblyDescription("Менеджер справ: завдання з пріоритетами, дедлайнами, фільтрами й пошуком")]
[assembly: AssemblyCompany("Білоус Владислав, група К-41")]
[assembly: AssemblyProduct("MyApp")]
[assembly: AssemblyCopyright("© 2026 Білоус Владислав")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace MyApp
{
    public class TaskItem
    {
        public string Title;
        public string Priority;
        public DateTime? Deadline;
        public bool Done;
        public DateTime Created;
    }

    /// <summary>Бізнес-логіка без інтерфейсу: список завдань, фільтри, статистика, збереження у TSV.</summary>
    public class TaskStore
    {
        public static readonly string[] Priorities = { "Високий", "Середній", "Низький" };
        public const string DateFormat = "dd.MM.yyyy";
        public readonly List<TaskItem> Items = new List<TaskItem>();
        public readonly string FilePath;

        public TaskStore(string filePath) { FilePath = filePath; }

        public TaskItem Add(string title, string priority, DateTime? deadline)
        {
            title = (title ?? "").Trim();
            if (title.Length == 0) throw new ArgumentException("Введіть текст завдання.");
            if (title.Length > 200) throw new ArgumentException("Текст завдання задовгий (понад 200 символів).");
            if (Array.IndexOf(Priorities, priority) < 0) throw new ArgumentException("Невідомий пріоритет: " + priority);
            var t = new TaskItem { Title = title, Priority = priority, Deadline = deadline, Created = DateTime.Now };
            Items.Add(t);
            return t;
        }

        public void Update(TaskItem t, string title, string priority, DateTime? deadline)
        {
            var probe = new TaskStore(null);
            probe.Add(title, priority, deadline);           // та сама валідація, що й під час додавання
            t.Title = title.Trim(); t.Priority = priority; t.Deadline = deadline;
        }

        public void Toggle(TaskItem t) { t.Done = !t.Done; }
        public void Remove(TaskItem t) { Items.Remove(t); }
        public int ClearDone() { return Items.RemoveAll(x => x.Done); }

        public static bool IsOverdue(TaskItem t, DateTime today)
        {
            return !t.Done && t.Deadline.HasValue && t.Deadline.Value.Date < today.Date;
        }

        public IEnumerable<TaskItem> Filter(string mode, string query)
        {
            query = (query ?? "").Trim().ToLower(CultureInfo.CurrentCulture);
            return Items.Where(t => (mode == "active" ? !t.Done : mode == "done" ? t.Done : true)
                                    && (query.Length == 0 || t.Title.ToLower(CultureInfo.CurrentCulture).Contains(query)));
        }

        public int[] Stats(DateTime today)
        {
            int done = Items.Count(t => t.Done);
            return new[] { Items.Count, Items.Count - done, done, Items.Count(t => IsOverdue(t, today)) };
        }

        static string Esc(string s) { return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", ""); }

        static string Unesc(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    char c = s[++i];
                    sb.Append(c == 't' ? '\t' : c == 'n' ? '\n' : c);
                }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            var sb = new StringBuilder("title\tpriority\tdeadline\tdone\tcreated\n");
            foreach (var t in Items)
                sb.Append(Esc(t.Title)).Append('\t').Append(t.Priority).Append('\t')
                  .Append(t.Deadline.HasValue ? t.Deadline.Value.ToString(DateFormat, CultureInfo.InvariantCulture) : "")
                  .Append('\t').Append(t.Done ? "1" : "0").Append('\t')
                  .Append(t.Created.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)).Append('\n');
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);                       // атомарна заміна: файл не пошкодиться при збої
        }

        public bool Load()
        {
            Items.Clear();
            if (!File.Exists(FilePath)) return false;
            foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8).Skip(1))
            {
                var p = line.Split(new[] { '\t' });           // Split(char[]) є в .NET Framework 4.x
                if (p.Length < 5) continue;
                DateTime d, c;
                Items.Add(new TaskItem
                {
                    Title = Unesc(p[0]),
                    Priority = Array.IndexOf(Priorities, p[1]) >= 0 ? p[1] : "Середній",
                    Deadline = DateTime.TryParseExact(p[2], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ? (DateTime?)d : null,
                    Done = p[3] == "1",
                    Created = DateTime.TryParseExact(p[4], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out c) ? c : DateTime.Now
                });
            }
            return true;
        }
    }

    /// <summary>Діалог редагування завдання.</summary>
    public class EditDialog : Form
    {
        public readonly TextBox Title = new TextBox { Width = 330 };
        public readonly ComboBox Priority = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        public readonly DateTimePicker Deadline = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = TaskStore.DateFormat, ShowCheckBox = true, Width = 140 };

        public EditDialog(TaskItem t)
        {
            Text = "Редагування завдання";
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Font = new Font("Segoe UI", 9.75f);
            Priority.Items.AddRange(TaskStore.Priorities);
            Title.Text = t.Title; Priority.SelectedItem = t.Priority;
            Deadline.Checked = t.Deadline.HasValue; Deadline.Value = t.Deadline ?? DateTime.Today;
            var ok = new Button { Text = "Зберегти", DialogResult = DialogResult.OK, Width = 100 };
            var cancel = new Button { Text = "Скасувати", DialogResult = DialogResult.Cancel, Width = 100 };
            AcceptButton = ok; CancelButton = cancel;
            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Padding = new Padding(10) };
            grid.Controls.Add(new Label { Text = "Завдання:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            grid.Controls.Add(Title, 1, 0);
            grid.Controls.Add(new Label { Text = "Пріоритет:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            grid.Controls.Add(Priority, 1, 1);
            grid.Controls.Add(new Label { Text = "Дедлайн:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            grid.Controls.Add(Deadline, 1, 2);
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
            buttons.Controls.Add(cancel); buttons.Controls.Add(ok);
            grid.Controls.Add(buttons, 1, 3);
            Controls.Add(grid);
        }
    }

    public class MainForm : Form
    {
        readonly TaskStore store;
        readonly TextBox txtTitle = new TextBox { Width = 250 };
        readonly ComboBox cmbPriority = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        readonly DateTimePicker dtpDeadline = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = TaskStore.DateFormat, ShowCheckBox = true, Checked = false, Width = 130 };
        readonly RadioButton rbAll = new RadioButton { Text = "Усі", Checked = true, AutoSize = true };
        readonly RadioButton rbActive = new RadioButton { Text = "Активні", AutoSize = true };
        readonly RadioButton rbDone = new RadioButton { Text = "Виконані", AutoSize = true };
        readonly TextBox txtSearch = new TextBox { Width = 200 };
        readonly ListView list = new ListView { View = View.Details, FullRowSelect = true, GridLines = true, HideSelection = false, Dock = DockStyle.Fill, MultiSelect = false };
        readonly ToolStripStatusLabel status = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        int sortColumn = -1; bool sortAsc = true;

        public MainForm(TaskStore s)
        {
            store = s;
            Text = "MyApp — менеджер справ " + Program.Version;
            Font = new Font("Segoe UI", 9.75f);
            Size = new Size(860, 560); MinimumSize = new Size(700, 440); StartPosition = FormStartPosition.CenterScreen;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }

            var menu = new MenuStrip();
            var mFile = new ToolStripMenuItem("Файл");
            mFile.DropDownItems.Add(new ToolStripMenuItem("Зберегти", null, (o, e) => SaveAndRefresh(), Keys.Control | Keys.S));
            mFile.DropDownItems.Add(new ToolStripMenuItem("Відкрити теку з даними", null, (o, e) => OpenDataFolder()));
            mFile.DropDownItems.Add(new ToolStripSeparator());
            mFile.DropDownItems.Add(new ToolStripMenuItem("Вихід", null, (o, e) => Close(), Keys.Alt | Keys.F4));
            var mHelp = new ToolStripMenuItem("Довідка");
            mHelp.DropDownItems.Add(new ToolStripMenuItem("Про програму", null, (o, e) => About(), Keys.F1));
            menu.Items.Add(mFile); menu.Items.Add(mHelp);
            MainMenuStrip = menu;

            cmbPriority.Items.AddRange(TaskStore.Priorities); cmbPriority.SelectedIndex = 1;
            var btnAdd = new Button { Text = "Додати", AutoSize = true };
            btnAdd.Click += (o, e) => AddTask();
            AcceptButton = btnAdd;
            var rowNew = Row(new Label { Text = "Завдання:", AutoSize = true, Margin = new Padding(3, 7, 3, 3) }, txtTitle,
                             new Label { Text = "Пріоритет:", AutoSize = true, Margin = new Padding(8, 7, 3, 3) }, cmbPriority,
                             new Label { Text = "Дедлайн:", AutoSize = true, Margin = new Padding(8, 7, 3, 3) }, dtpDeadline, btnAdd);
            var rowFilter = Row(new Label { Text = "Показати:", AutoSize = true, Margin = new Padding(3, 7, 3, 3) }, rbAll, rbActive, rbDone,
                                new Label { Text = "Пошук:", AutoSize = true, Margin = new Padding(20, 7, 3, 3) }, txtSearch);
            foreach (var rb in new[] { rbAll, rbActive, rbDone }) rb.CheckedChanged += (o, e) => RefreshList();
            txtSearch.TextChanged += (o, e) => RefreshList();

            list.Columns.Add("✓", 34); list.Columns.Add("Завдання", 360); list.Columns.Add("Пріоритет", 110); list.Columns.Add("Дедлайн", 110);
            list.ColumnClick += (o, e) => { sortAsc = e.Column == sortColumn ? !sortAsc : true; sortColumn = e.Column; RefreshList(); };
            list.DoubleClick += (o, e) => EditTask();
            list.KeyDown += (o, e) => { if (e.KeyCode == Keys.Delete) DeleteTask(); if (e.KeyCode == Keys.Space) ToggleTask(); };
            list.Resize += (o, e) => { list.Columns[1].Width = Math.Max(160, list.ClientSize.Width - 34 - 110 - 110 - 4); };

            var bDone = new Button { Text = "✓ Виконано", AutoSize = true }; bDone.Click += (o, e) => ToggleTask();
            var bEdit = new Button { Text = "Редагувати", AutoSize = true }; bEdit.Click += (o, e) => EditTask();
            var bDel = new Button { Text = "Видалити", AutoSize = true }; bDel.Click += (o, e) => DeleteTask();
            var bClear = new Button { Text = "Очистити виконані", AutoSize = true }; bClear.Click += (o, e) => ClearDone();
            var rowButtons = Row(bDone, bEdit, bDel, bClear);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(6) };
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(rowNew, 0, 0); table.Controls.Add(rowFilter, 0, 1);
            table.Controls.Add(list, 0, 2); table.Controls.Add(rowButtons, 0, 3);
            var strip = new StatusStrip(); strip.Items.Add(status);
            Controls.Add(table); Controls.Add(strip); Controls.Add(menu);
            table.BringToFront();
            Load += (o, e) => RefreshList();
        }

        static FlowLayoutPanel Row(params Control[] items)
        {
            var p = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0, 2, 0, 2) };
            p.Controls.AddRange(items);
            return p;
        }

        string Mode() { return rbActive.Checked ? "active" : rbDone.Checked ? "done" : "all"; }

        TaskItem Selected() { return list.SelectedItems.Count == 0 ? null : (TaskItem)list.SelectedItems[0].Tag; }

        void Warn(string msg) { MessageBox.Show(this, msg, "MyApp", MessageBoxButtons.OK, MessageBoxIcon.Warning); }

        void RefreshList()
        {
            var today = DateTime.Today;
            var items = store.Filter(Mode(), txtSearch.Text).ToList();
            if (sortColumn >= 0)
            {
                Comparison<TaskItem> cmp;
                if (sortColumn == 0) cmp = (a, b) => a.Done.CompareTo(b.Done);
                else if (sortColumn == 2) cmp = (a, b) => Array.IndexOf(TaskStore.Priorities, a.Priority).CompareTo(Array.IndexOf(TaskStore.Priorities, b.Priority));
                else if (sortColumn == 3) cmp = (a, b) => (a.Deadline ?? DateTime.MaxValue).CompareTo(b.Deadline ?? DateTime.MaxValue);
                else cmp = (a, b) => string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase);
                items.Sort((a, b) => sortAsc ? cmp(a, b) : cmp(b, a));
            }
            list.BeginUpdate();
            list.Items.Clear();
            foreach (var t in items)
            {
                var it = new ListViewItem(new[] { t.Done ? "✓" : "", t.Title, t.Priority, t.Deadline.HasValue ? t.Deadline.Value.ToString(TaskStore.DateFormat) : "—" }) { Tag = t };
                if (t.Done) { it.ForeColor = Color.Gray; it.Font = new Font(list.Font, FontStyle.Strikeout); }
                else if (TaskStore.IsOverdue(t, today)) it.ForeColor = Color.Firebrick;
                else if (t.Priority == "Високий") it.Font = new Font(list.Font, FontStyle.Bold);
                list.Items.Add(it);
            }
            list.EndUpdate();
            var s = store.Stats(today);
            status.Text = string.Format("Усього: {0} · Активних: {1} · Виконано: {2} · Прострочено: {3} · Файл: {4}", s[0], s[1], s[2], s[3], store.FilePath);
        }

        void SaveAndRefresh()
        {
            try { store.Save(); }
            catch (Exception ex) { Warn("Не вдалося зберегти дані: " + ex.Message); }
            RefreshList();
        }

        void AddTask()
        {
            try
            {
                store.Add(txtTitle.Text, (string)cmbPriority.SelectedItem, dtpDeadline.Checked ? (DateTime?)dtpDeadline.Value.Date : null);
                txtTitle.Clear(); dtpDeadline.Checked = false; txtTitle.Focus();
                SaveAndRefresh();
            }
            catch (ArgumentException ex) { Warn(ex.Message); }
        }

        void ToggleTask()
        {
            var t = Selected();
            if (t == null) { Warn("Виберіть завдання у списку."); return; }
            store.Toggle(t); SaveAndRefresh();
        }

        void EditTask()
        {
            var t = Selected();
            if (t == null) { Warn("Виберіть завдання для редагування."); return; }
            using (var d = new EditDialog(t))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try { store.Update(t, d.Title.Text, (string)d.Priority.SelectedItem, d.Deadline.Checked ? (DateTime?)d.Deadline.Value.Date : null); SaveAndRefresh(); }
                catch (ArgumentException ex) { Warn(ex.Message); }
            }
        }

        void DeleteTask()
        {
            var t = Selected();
            if (t == null) { Warn("Виберіть завдання для видалення."); return; }
            if (MessageBox.Show(this, "Видалити завдання «" + t.Title + "»?", "Підтвердження", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { store.Remove(t); SaveAndRefresh(); }
        }

        void ClearDone()
        {
            int n = store.ClearDone();
            SaveAndRefresh();
            MessageBox.Show(this, n == 0 ? "Виконаних завдань немає." : "Видалено виконаних завдань: " + n, "MyApp", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void OpenDataFolder()
        {
            try { System.Diagnostics.Process.Start(Path.GetDirectoryName(store.FilePath)); } catch (Exception ex) { Warn(ex.Message); }
        }

        void About()
        {
            MessageBox.Show(this, "MyApp — менеджер справ, версія " + Program.Version + "\n\nАвтор: Білоус Владислав, група К-41\n" +
                            "Лабораторна робота № 12, ТСПП\n\nДані зберігаються у файлі:\n" + store.FilePath,
                            "Про програму", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public static class Program
    {
        public const string Version = "1.0.0";

        static string DataFile()
        {
            return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyApp"), "tasks.tsv");
        }

        static void Seed(TaskStore s)
        {
            s.Add("Ознайомитися з MyApp", "Середній", null);
            s.Add("Здати звіт з ЛР12 (ТСПП)", "Високий", DateTime.Today.AddDays(2));
            s.Add("Перевірити ярлик і видалення програми", "Низький", DateTime.Today.AddDays(5));
            s.Save();
        }

        /// <summary>Самоперевірка логіки без інтерфейсу: myapp.exe --selftest (використовується в CI).</summary>
        static int SelfTest()
        {
            int failed = 0;
            Action<string, bool> check = (name, ok) => { Console.WriteLine((ok ? "PASS " : "FAIL ") + name); if (!ok) failed++; };
            string path = Path.Combine(Path.GetTempPath(), "myapp_selftest_" + Guid.NewGuid().ToString("N") + ".tsv");
            var s = new TaskStore(path);
            var a = s.Add("Звіт\tз ЛР12", "Високий", DateTime.Today.AddDays(-1));
            s.Add("Купити квиток", "Низький", null);
            bool thrown = false;
            try { s.Add("   ", "Середній", null); } catch (ArgumentException) { thrown = true; }
            check("порожнє завдання відхилено", thrown);
            check("прострочене завдання виявлено", TaskStore.IsOverdue(a, DateTime.Today));
            s.Toggle(a);
            check("виконане завдання не прострочене", !TaskStore.IsOverdue(a, DateTime.Today));
            check("фільтр «Активні»", s.Filter("active", "").Count() == 1);
            check("пошук без урахування регістру", s.Filter("all", "КВИТОК").Count() == 1);
            s.Save();
            var s2 = new TaskStore(path); s2.Load();
            check("збереження й завантаження (TSV з табуляцією в тексті)", s2.Items.Count == 2 && s2.Items[0].Title == "Звіт\tз ЛР12" && s2.Items[0].Done);
            check("статистика", string.Join(",", s2.Stats(DateTime.Today).Select(x => x.ToString()).ToArray()) == "2,1,1,0");
            check("очищення виконаних", s2.ClearDone() == 1 && s2.Items.Count == 1);
            File.Delete(path);
            Console.WriteLine(failed == 0 ? "OK: усі перевірки пройдено" : "ПОМИЛОК: " + failed);
            return failed == 0 ? 0 : 1;
        }

        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--selftest") return SelfTest();
            if (args.Length > 0 && args[0] == "--version") { Console.WriteLine("MyApp " + Version); return 0; }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var store = new TaskStore(DataFile());
            try { if (!store.Load()) Seed(store); }
            catch (Exception ex) { MessageBox.Show("Не вдалося прочитати дані: " + ex.Message, "MyApp", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            Application.Run(new MainForm(store));
            return 0;
        }
    }
}
