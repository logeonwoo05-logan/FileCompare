namespace FileCompare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitializeListViewDrawEvents();
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

        private string FormatSizeInKb(long bytes)
        {
            return $"{(bytes / 1024.0):N0} KB";
        }

        private void CompareAndPopulate()
        {
            bool leftExists = !string.IsNullOrWhiteSpace(txtLeftDir.Text) && Directory.Exists(txtLeftDir.Text);
            bool rightExists = !string.IsNullOrWhiteSpace(txtRightDir.Text) && Directory.Exists(txtRightDir.Text);

            var leftFiles = leftExists ? new DirectoryInfo(txtLeftDir.Text).GetFiles().OrderBy(f => f.Name).ToArray() : new FileInfo[0];
            var rightFiles = rightExists ? new DirectoryInfo(txtRightDir.Text).GetFiles().OrderBy(f => f.Name).ToArray() : new FileInfo[0];

            PopulateList(lvwLeftDir, leftFiles, rightFiles);
            PopulateList(lvwRightDir, rightFiles, leftFiles);
        }

        private void PopulateList(ListView lv, FileInfo[] sourceFiles, FileInfo[] targetFiles)
        {
            lv.BeginUpdate();
            lv.Items.Clear();

            try
            {
                foreach (var lf in sourceFiles)
                {
                    var litem = new ListViewItem(lf.Name);
                    litem.SubItems.Add(FormatSizeInKb(lf.Length));
                    litem.SubItems.Add(lf.LastWriteTime.ToString("g"));

                    var rf = targetFiles.FirstOrDefault(t => t.Name.Equals(lf.Name, StringComparison.OrdinalIgnoreCase));

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
