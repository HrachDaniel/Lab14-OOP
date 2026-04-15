using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab14_OOP
{
    public partial class Form1 : Form
    {
        // =========================================================
        // ГЛОБАЛЬНІ ЗМІННІ (доступні в усьому класі Form1)
        // =========================================================

        // Зберігає шлях до папки, яка зараз відкрита (наприклад, "C:\Windows")
        private string currentPath = "";

        // Зберігає повний шлях до файлу або папки, на які користувач клікнув у списку
        private string selectedItemPath = "";

        // Прапорець: true, якщо виділено папку, false - якщо виділено файл
        private bool isFolderSelected = false;

        public Form1()
        {
            InitializeComponent(); // Малює всі кнопки та списки на формі

            // Вказуємо, що при завантаженні форми треба виконати метод Form1_Load
            this.Load += Form1_Load;
        }

        // =========================================================
        // 1. ЗАВАНТАЖЕННЯ ДИСКІВ ТА НАВІГАЦІЯ
        // =========================================================

        // Спрацьовує автоматично під час запуску програми
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Отримуємо масив усіх дисків на комп'ютері
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    // Додаємо в список лише ті диски, які готові до роботи (не порожні дисководи)
                    if (drive.IsReady) cmbDrives.Items.Add(drive.Name);
                }
                // Якщо знайдено хоча б один диск, автоматично вибираємо перший (індекс 0)
                if (cmbDrives.Items.Count > 0) cmbDrives.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show("Помилка дисків: " + ex.Message); }
        }

        // Цей метод (або його дублікат нижче) спрацьовує, коли користувач обирає інший диск у списку
        private void cmbDrives_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Беремо назву обраного диска і завантажуємо його вміст
            LoadDirectory(cmbDrives.SelectedItem.ToString());
        }

        // Головний метод, який сканує папку і виводить її вміст на екран
        private void LoadDirectory(string path)
        {
            // Захист: якщо шлях порожній, просто виходимо з методу, щоб уникнути помилок
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                currentPath = path; // Запам'ятовуємо, де ми зараз знаходимося
                lstFolders.Items.Clear(); // Очищаємо старий список папок
                lstFiles.Items.Clear();   // Очищаємо старий список файлів
                selectedItemPath = "";    // Скидаємо виділення

                // Зчитуємо фільтри з текстових полів. Якщо там пусто, ставимо "*" (показувати все)
                string folderFilter = string.IsNullOrWhiteSpace(txtFolderFilter.Text) ? "*" : txtFolderFilter.Text;
                string fileFilter = string.IsNullOrWhiteSpace(txtFileFilter.Text) ? "*.*" : txtFileFilter.Text;

                // Отримуємо всі папки за вказаним шляхом, які відповідають фільтру
                foreach (string d in Directory.GetDirectories(path, folderFilter))
                    lstFolders.Items.Add(Path.GetFileName(d)); // Додаємо в список лише назву папки, а не весь шлях

                // Отримуємо всі файли за вказаним шляхом, які відповідають фільтру
                foreach (string f in Directory.GetFiles(path, fileFilter))
                    lstFiles.Items.Add(Path.GetFileName(f)); // Додаємо лише ім'я файлу
            }
            catch (UnauthorizedAccessException)
            {
                // Якщо Windows не пускає нас у системну папку, показуємо повідомлення і повертаємось назад
                MessageBox.Show("Відмовлено в доступі до цієї папки!", "Помилка");
                NavigateUp();
            }
            catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
        }

        // Кнопка "Вгору"
        private void btnUp_Click(object sender, EventArgs e) => NavigateUp();

        // Метод для переходу в батьківську (попередню) папку
        private void NavigateUp()
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                // Отримуємо інформацію про папку, яка знаходиться на рівень вище
                DirectoryInfo parent = Directory.GetParent(currentPath);

                // Якщо така папка існує (ми не в корені диска), завантажуємо її
                if (parent != null) LoadDirectory(parent.FullName);
            }
        }

        // Подвійний клік по папці у списку
        private void lstFolders_DoubleClick(object sender, EventArgs e)
        {
            if (lstFolders.SelectedItem != null)
                // Зліплюємо поточний шлях і назву папки, щоб отримати новий шлях, і заходимо туди
                LoadDirectory(Path.Combine(currentPath, lstFolders.SelectedItem.ToString()));
        }

        // =========================================================
        // 2. ВИДІЛЕННЯ ТА ПЕРЕГЛЯД ВЛАСТИВОСТЕЙ / ФАЙЛІВ
        // =========================================================

        // Клік по папці (один раз)
        private void lstFolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstFolders.SelectedItem == null) return;
            selectedItemPath = Path.Combine(currentPath, lstFolders.SelectedItem.ToString());
            isFolderSelected = true; // Фіксуємо, що вибрано саме папку
            ShowProperties(selectedItemPath, true); // Показуємо властивості
        }

        // Клік по файлу (один раз)
        private void lstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem == null) return;
            selectedItemPath = Path.Combine(currentPath, lstFiles.SelectedItem.ToString());
            isFolderSelected = false; // Фіксуємо, що вибрано файл
            ShowProperties(selectedItemPath, false); // Показуємо властивості
            PreviewFile(selectedItemPath); // Пробуємо відкрити картинку або текст
        }

        // Виведення властивостей у текстове поле на першій вкладці
        private void ShowProperties(string path, bool isFolder)
        {
            try
            {
                txtProperties.Text = $"=== ВЛАСТИВОСТІ ===\r\nШлях: {path}\r\n";
                if (!isFolder)
                {
                    // Якщо це файл, використовуємо FileInfo
                    FileInfo fi = new FileInfo(path);
                    txtProperties.Text += $"Розмір: {fi.Length} байт\r\nСтворено: {fi.CreationTime}\r\n";
                    UpdateAttributeCheckboxes(fi.Attributes); // Оновлюємо галочки (Прихований/Читання)
                }
                else
                {
                    // Якщо це папка, використовуємо DirectoryInfo
                    DirectoryInfo di = new DirectoryInfo(path);
                    txtProperties.Text += $"Створено: {di.CreationTime}\r\n";
                    UpdateAttributeCheckboxes(di.Attributes);
                }

                // ЧИТАННЯ АТРИБУТІВ БЕЗПЕКИ (Access Control List)
                txtProperties.Text += "\r\n=== АТРИБУТИ БЕЗПЕКИ (ACL) ===\r\n";
                if (isFolder)
                {
                    DirectorySecurity ds = new DirectoryInfo(path).GetAccessControl();
                    foreach (FileSystemAccessRule rule in ds.GetAccessRules(true, true, typeof(NTAccount)))
                        txtProperties.Text += $"- {rule.IdentityReference}: {rule.FileSystemRights}\r\n";
                }
                else
                {
                    FileSecurity fs = new FileInfo(path).GetAccessControl();
                    foreach (FileSystemAccessRule rule in fs.GetAccessRules(true, true, typeof(NTAccount)))
                        txtProperties.Text += $"- {rule.IdentityReference}: {rule.FileSystemRights}\r\n";
                }
                tabControl1.SelectedIndex = 0; // Перемикаємось на вкладку з властивостями
            }
            catch { txtProperties.Text += "\n[Немає доступу до ACL]"; } // Якщо Windows забороняє читати права
        }

        // Попередній перегляд картинок і тексту
        private void PreviewFile(string path)
        {
            // Очищаємо попередню картинку та текст, щоб вони не залишалися від старого файлу
            if (picPreview.Image != null) { picPreview.Image.Dispose(); picPreview.Image = null; }
            txtPreview.Text = "";

            // Отримуємо розширення файлу малими літерами (наприклад, ".jpg")
            string ext = Path.GetExtension(path).ToLower();

            // Якщо це зображення
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
            {
                try { picPreview.Image = Image.FromFile(path); tabControl1.SelectedIndex = 2; } catch { }
            }
            // Якщо це текстовий файл або код
            else if (ext == ".txt" || ext == ".log" || ext == ".xml" || ext == ".json" || ext == ".cs")
            {
                try
                {
                    // Читаємо файл, тільки якщо він менший за 1 МБ (1048576 байт), щоб програма не зависла
                    if (new FileInfo(path).Length < 1048576) { txtPreview.Text = File.ReadAllText(path); tabControl1.SelectedIndex = 1; }
                    else txtPreview.Text = "Файл занадто великий.";
                }
                catch { }
            }
            // Якщо формат невідомий, просто перемикаємось на вкладку з властивостями
            else tabControl1.SelectedIndex = 0;
        }

        // =========================================================
        // 3. СТВОРЕННЯ, ВИДАЛЕННЯ, КОПІЮВАННЯ ТА ПЕРЕМІЩЕННЯ
        // =========================================================

        private void btnCreateFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentPath)) { MessageBox.Show("Спочатку оберіть диск!"); return; }
            if (string.IsNullOrWhiteSpace(txtInputName.Text)) { MessageBox.Show("Введіть назву в поле!"); return; }

            try
            {
                // Створюємо папку
                Directory.CreateDirectory(Path.Combine(currentPath, txtInputName.Text));
                LoadDirectory(currentPath); // Оновлюємо списки
            }
            catch (Exception ex) { MessageBox.Show("Помилка створення: " + ex.Message); }
        }

        private void btnCreateFile_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInputName.Text)) { MessageBox.Show("Введіть назву в поле txtInputName"); return; }
            // Створюємо порожній файл і одразу закриваємо до нього доступ (.Close()), щоб він не був заблокований
            File.Create(Path.Combine(currentPath, txtInputName.Text)).Close();
            LoadDirectory(currentPath);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedItemPath)) return; // Нічого не вибрано

            // Запитуємо підтвердження у користувача
            if (MessageBox.Show("Видалити " + selectedItemPath + "?", "Підтвердження", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    // Видаляємо папку з усім вмістом (true) або просто файл
                    if (isFolderSelected) Directory.Delete(selectedItemPath, true);
                    else File.Delete(selectedItemPath);
                    LoadDirectory(currentPath);
                }
                catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedItemPath)) return;

            // Відкриваємо стандартне вікно Windows для вибору папки, куди копіювати
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Виберіть папку для копіювання";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    // Формуємо повний шлях призначення
                    string destPath = Path.Combine(fbd.SelectedPath, Path.GetFileName(selectedItemPath));
                    try
                    {
                        if (isFolderSelected) DirectoryCopy(selectedItemPath, destPath, true); // Викликаємо спеціальний метод для папок
                        else File.Copy(selectedItemPath, destPath, true); // Для файлів є стандартна команда (true - перезаписати)
                        MessageBox.Show("Скопійовано!");
                        LoadDirectory(currentPath);
                    }
                    catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
                }
            }
        }

        private void btnMove_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedItemPath)) return;
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Виберіть папку для переміщення";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string destPath = Path.Combine(fbd.SelectedPath, Path.GetFileName(selectedItemPath));
                    try
                    {
                        if (isFolderSelected) Directory.Move(selectedItemPath, destPath);
                        else File.Move(selectedItemPath, destPath);
                        MessageBox.Show("Переміщено!");
                        LoadDirectory(currentPath);
                    }
                    catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
                }
            }
        }

        // Окремий рекурсивний метод для копіювання папок з їхнім вмістом
        private void DirectoryCopy(string sourceDir, string destDir, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            Directory.CreateDirectory(destDir); // Спочатку створюємо саму цільову папку

            // Копіюємо всі файли з неї
            foreach (FileInfo file in dir.GetFiles()) file.CopyTo(Path.Combine(destDir, file.Name), false);

            // Якщо вказано copySubDirs = true, рекурсивно копіюємо всі вкладені папки
            if (copySubDirs)
                foreach (DirectoryInfo subdir in dir.GetDirectories())
                    DirectoryCopy(subdir.FullName, Path.Combine(destDir, subdir.Name), copySubDirs);
        }

        // =========================================================
        // 4. РОБОТА З ТЕКСТОМ ТА АТРИБУТАМИ
        // =========================================================

        // Збереження змін у текстовому файлі
        private void btnSaveText_Click(object sender, EventArgs e)
        {
            if (!isFolderSelected && File.Exists(selectedItemPath))
            {
                try { File.WriteAllText(selectedItemPath, txtPreview.Text); MessageBox.Show("Текст збережено!"); }
                catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
            }
        }

        // Оновлює галочки CheckBox відповідно до атрибутів обраного файлу
        private void UpdateAttributeCheckboxes(FileAttributes attr)
        {
            // Побітове "І" (&) перевіряє, чи є відповідний атрибут у файлу
            chkReadOnly.Checked = (attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            chkHidden.Checked = (attr & FileAttributes.Hidden) == FileAttributes.Hidden;
        }

        // Застосування нових атрибутів до файлу
        private void btnApplyAttributes_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedItemPath)) return;
            try
            {
                // Отримуємо поточні атрибути
                FileAttributes attr = isFolderSelected ? new DirectoryInfo(selectedItemPath).Attributes : File.GetAttributes(selectedItemPath);

                // Якщо галочка стоїть, додаємо атрибут (|=). Якщо знята, видаляємо (&= ~)
                if (chkReadOnly.Checked) attr |= FileAttributes.ReadOnly; else attr &= ~FileAttributes.ReadOnly;
                if (chkHidden.Checked) attr |= FileAttributes.Hidden; else attr &= ~FileAttributes.Hidden;

                // Застосовуємо оновлені атрибути
                if (isFolderSelected) new DirectoryInfo(selectedItemPath).Attributes = attr;
                else File.SetAttributes(selectedItemPath, attr);

                MessageBox.Show("Атрибути оновлено!");
                ShowProperties(selectedItemPath, isFolderSelected);
            }
            catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
        }

        // =========================================================
        // 5. ZIP АРХІВАЦІЯ ТА РОЗПАКУВАННЯ
        // =========================================================

        private void btnZip_Click(object sender, EventArgs e)
        {
            // Архівувати можна тільки папки
            if (string.IsNullOrEmpty(selectedItemPath) || !isFolderSelected)
            {
                MessageBox.Show("Виберіть ПАПКУ для створення архіву!"); return;
            }

            // Відкриваємо вікно для збереження ZIP-файлу
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "ZIP Архів|*.zip", FileName = Path.GetFileName(selectedItemPath) + ".zip" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try { ZipFile.CreateFromDirectory(selectedItemPath, sfd.FileName); MessageBox.Show("Архів створено!"); LoadDirectory(currentPath); }
                    catch (Exception ex) { MessageBox.Show("Помилка: " + ex.Message); }
                }
            }
        }

        private void btnUnzip_Click(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem == null)
            {
                MessageBox.Show("Будь ласка, оберіть файл для розпакування у списку файлів.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedFileName = lstFiles.SelectedItem.ToString();
            string fullZipPath = Path.Combine(currentPath, selectedFileName);

            // Перевіряємо, чи має файл розширення .zip
            if (Path.GetExtension(fullZipPath).ToLower() != ".zip")
            {
                MessageBox.Show("Обраний файл не є ZIP-архівом!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Формуємо ім'я папки для розпакування (ім'я архіву без ".zip")
                string folderName = Path.GetFileNameWithoutExtension(selectedFileName);
                string extractPath = Path.Combine(currentPath, folderName);

                // Якщо папки ще немає, створюємо її
                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }

                // Витягуємо вміст архіву у створену папку
                ZipFile.ExtractToDirectory(fullZipPath, extractPath);

                MessageBox.Show($"Архів успішно розпаковано у папку:\n{folderName}", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDirectory(currentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Виникла помилка під час розпакування:\n" + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================
        // 6. ФІЛЬТРАЦІЯ
        // =========================================================

        // Спрацьовує автоматично, коли ви вводите текст у поля фільтрів
        private void txtFolderFilter_TextChanged(object sender, EventArgs e) { if (Directory.Exists(currentPath)) LoadDirectory(currentPath); }
        private void txtFileFilter_TextChanged(object sender, EventArgs e) { if (Directory.Exists(currentPath)) LoadDirectory(currentPath); }

    }
}