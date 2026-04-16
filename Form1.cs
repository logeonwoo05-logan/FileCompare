namespace FileCompare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitializeListViewDrawEvents();

            // 디자이너 속성 덮어쓰기 방지를 위해 명시적으로 이벤트 재할당
            btnCopyFromLeft.Click -= btnCopyFromLeft_Click;
            btnCopyFromLeft.Click += btnCopyFromLeft_Click;
            btnCopyFromRight.Click -= btnCopyFromRight_Click;
            btnCopyFromRight.Click += btnCopyFromRight_Click;
        }

        private void InitializeListViewDrawEvents()
        {
            lvwLeftDir.OwnerDraw = true;
            lvwLeftDir.DrawColumnHeader += Lvw_DrawColumnHeader;
            lvwLeftDir.DrawItem += Lvw_DrawItem;
            lvwLeftDir.DrawSubItem += Lvw_DrawSubItem;

            lvwRightDir.OwnerDraw = true;
            lvwRightDir.DrawColumnHeader += Lvw_DrawColumnHeader;
            lvwRightDir.DrawItem += Lvw_DrawItem;
            lvwRightDir.DrawSubItem += Lvw_DrawSubItem;
        }

        private void Lvw_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void Lvw_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // 1단계: DrawItem 이벤트 사용
            // e.DrawDefault = false; by default unless set to true, handled in DrawSubItem
        }

        private void Lvw_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            // 3단계: 사용자 정의 출력
            e.DrawBackground();
            Color foreColor = e.Item.ForeColor;
            if (e.Item.Selected)
            {
                e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
                foreColor = SystemColors.HighlightText;
            }

            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font, e.Bounds, foreColor, flags);
        }

        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";

                // 현재 텍스트박스에 있는 경로를 초기 선택 폴더로 설정
                if (!string.IsNullOrWhiteSpace(txtLeftDir.Text) &&
                Directory.Exists(txtLeftDir.Text))
                {
                    dlg.SelectedPath = txtLeftDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;
                    CompareAndPopulate();

                }
            }
        }

        private void btnRightDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";

                // 현재 텍스트박스에 있는 경로를 초기 선택 폴더로 설정
                if (!string.IsNullOrWhiteSpace(txtRightDir.Text) &&
                Directory.Exists(txtRightDir.Text))
                {
                    dlg.SelectedPath = txtRightDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;
                    CompareAndPopulate();
                }
            }
        }

        private void btnCopyFromLeft_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLeftDir.Text) || string.IsNullOrWhiteSpace(txtRightDir.Text)) return;
            if (!Directory.Exists(txtLeftDir.Text) || !Directory.Exists(txtRightDir.Text)) return;

            var leftFiles = new DirectoryInfo(txtLeftDir.Text).GetFileSystemInfos().ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

            bool copied = false;
            foreach (ListViewItem item in lvwLeftDir.SelectedItems)
            {
                var name = item.Text;
                if (!leftFiles.TryGetValue(name, out var src))
                    continue; // 파일 정보 없으면 건너뜀

                var destPath = Path.Combine(txtRightDir.Text, src.Name);
                if (CopyItemWithConfirmation(src, destPath))
                {
                    copied = true;
                }
            }

            if (copied)
                CompareAndPopulate();
        }

        private void btnCopyFromRight_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLeftDir.Text) || string.IsNullOrWhiteSpace(txtRightDir.Text)) return;
            if (!Directory.Exists(txtLeftDir.Text) || !Directory.Exists(txtRightDir.Text)) return;

            var rightFiles = new DirectoryInfo(txtRightDir.Text).GetFileSystemInfos().ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

            bool copied = false;
            foreach (ListViewItem item in lvwRightDir.SelectedItems)
            {
                var name = item.Text;
                if (!rightFiles.TryGetValue(name, out var src))
                    continue;

                var destPath = Path.Combine(txtLeftDir.Text, src.Name);
                if (CopyItemWithConfirmation(src, destPath))
                {
                    copied = true;
                }
            }

            if (copied)
                CompareAndPopulate();
        }

        private bool CopyItemWithConfirmation(FileSystemInfo srcFsInfo, string destPath)
        {
            try
            {
                if (srcFsInfo is DirectoryInfo srcDir)
                {
                    if (Directory.Exists(destPath))
                    {
                        var destDir = new DirectoryInfo(destPath);
                        if (srcDir.LastWriteTime < destDir.LastWriteTime)
                        {
                            string msg = $"대상에 동일한 이름의 폴더가 이미 있습니다.\r\n" +
                                         $"대상 폴더가 더 신규 폴더입니다. 덮어쓰시겠습니까?\r\n\r\n" +
                                         $"원본: {srcDir.FullName}\r\n수정일: {srcDir.LastWriteTime:yyyy-MM-dd HH:mm:ss}\r\n\r\n" +
                                         $"대상: {destDir.FullName}\r\n수정일: {destDir.LastWriteTime:yyyy-MM-dd HH:mm:ss}";

                            var result = MessageBox.Show(msg, "덮어쓰기 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (result == DialogResult.No)
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(destPath);
                    }

                    bool anyCopied = true;
                    foreach (var item in srcDir.GetFileSystemInfos())
                    {
                        if (!CopyItemWithConfirmation(item, Path.Combine(destPath, item.Name)))
                        {
                            anyCopied = false;
                        }
                    }

                    // 폴더 내부 복사가 끝난 후 대상 폴더의 시간 정보를 원본과 동일하게 맞춤
                    try
                    {
                        Directory.SetCreationTime(destPath, srcDir.CreationTime);
                        Directory.SetLastWriteTime(destPath, srcDir.LastWriteTime);
                        Directory.SetLastAccessTime(destPath, srcDir.LastAccessTime);
                    }
                    catch
                    {
                        // 시간 설정 중 권한 문제 등이 있을 수 있으므로 오류를 무시합니다.
                    }

                    return anyCopied;
                }
                else if (srcFsInfo is FileInfo srcFile)
                {
                    return CopyFileWithConfirmation(srcFile.FullName, destPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"(하위항목) 복사 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return false;
        }

        private bool CopyFileWithConfirmation(string srcPath, string destPath)
        {
            var srcFile = new FileInfo(srcPath);

            if (File.Exists(destPath))
            {
                var destFile = new FileInfo(destPath);

                // 원본이 과거 파일이고 대상이 더 신규 파일인 경우에만 확인 메시지 창 표시
                // 과거 파일(오래된 것)을 현재 최신 파일(새로운 것)에 덮어쓸 때만 물어봄
                if (srcFile.LastWriteTime < destFile.LastWriteTime)
                {
                    string msg = $"대상에 동일한 이름의 파일이 이미 있습니다.\r\n" +
                                 $"대상 파일이 더 신규 파일입니다. 덮어쓰시겠습니까?\r\n\r\n" +
                                 $"원본: {srcFile.FullName}\r\n수정일: {srcFile.LastWriteTime:yyyy-MM-dd HH:mm:ss}\r\n\r\n" +
                                 $"대상: {destFile.FullName}\r\n수정일: {destFile.LastWriteTime:yyyy-MM-dd HH:mm:ss}";

                    var result = MessageBox.Show(msg, "덮어쓰기 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                    {
                        return false;
                    }
                }
            }

            try
            {
                File.Copy(srcPath, destPath, true);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 복사 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private string FormatSizeInKb(long bytes)
        {
            return $"{(bytes / 1024.0):N0} KB";
        }

        private void CompareAndPopulate()
        {
            bool leftExists = !string.IsNullOrWhiteSpace(txtLeftDir.Text) && Directory.Exists(txtLeftDir.Text);
            bool rightExists = !string.IsNullOrWhiteSpace(txtRightDir.Text) && Directory.Exists(txtRightDir.Text);

            var leftFiles = leftExists ? new DirectoryInfo(txtLeftDir.Text).GetFileSystemInfos().OrderBy(f => f is DirectoryInfo ? 0 : 1).ThenBy(f => f.Name).ToArray() : new FileSystemInfo[0];
            var rightFiles = rightExists ? new DirectoryInfo(txtRightDir.Text).GetFileSystemInfos().OrderBy(f => f is DirectoryInfo ? 0 : 1).ThenBy(f => f.Name).ToArray() : new FileSystemInfo[0];

            PopulateList(lvwLeftDir, leftFiles, rightFiles);
            PopulateList(lvwRightDir, rightFiles, leftFiles);
        }

        private void PopulateList(ListView lv, FileSystemInfo[] sourceFiles, FileSystemInfo[] targetFiles)
        {
            lv.BeginUpdate();
            lv.Items.Clear();

            try
            {
                foreach (var lf in sourceFiles)
                {
                    var litem = new ListViewItem(lf.Name);

                    if (lf is DirectoryInfo)
                    {
                        litem.SubItems.Add("<DIR>");
                    }
                    else
                    {
                        litem.SubItems.Add(FormatSizeInKb(((FileInfo)lf).Length));
                    }

                    litem.SubItems.Add(lf.LastWriteTime.ToString("g"));

                    var rf = targetFiles.FirstOrDefault(t => t.Name.Equals(lf.Name, StringComparison.OrdinalIgnoreCase) && t.GetType() == lf.GetType());

                    // 상태 결정 및 색상 적용
                    if (rf != null)
                    {
                        if (lf.LastWriteTime == rf.LastWriteTime)
                        {
                            litem.ForeColor = Color.Black;
                        }
                        else if (lf.LastWriteTime > rf.LastWriteTime)
                        {
                            litem.ForeColor = Color.Red;
                        }
                        else
                        {
                            litem.ForeColor = Color.Gray;
                        }
                    }
                    else
                    {
                        litem.ForeColor = Color.Purple;
                    }

                    lv.Items.Add(litem);
                }

                // 원래 Designer에서 설정된 넓직한 열 너비를 유지하기 위해
                // 자동 크기 조정 로직을 제거했습니다.
            }
            finally
            {
                lv.EndUpdate();
            }
        }
    }
}
