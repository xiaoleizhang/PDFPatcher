﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using PDFPatcher.Common;
using PDFPatcher.Model;

namespace PDFPatcher.Functions
{
	public partial class FontFilterForm : Form
	{
		sealed class FilterSetting
		{
			internal string FontName { get; }
			internal bool FullMatch { get; }
			internal float Size { get; }
			public FilterSetting (string fontName, bool fullMatch, float size) {
				FontName = fontName;
				FullMatch = fullMatch;
				Size = size;
			}
		}

		XmlElement _fontInfo;
		internal AutoBookmarkCondition[] FilterConditions {
			get;
			private set;
		}
		public FontFilterForm (XmlNode fontInfo) {
			InitializeComponent ();
			_fontInfo = fontInfo as XmlElement;

			var tcr = _FontInfoBox.TreeColumnRenderer as BrightIdeasSoftware.TreeListView.TreeRenderer;
			tcr.LinePen = new Pen (SystemColors.ControlDark);
			tcr.LinePen.DashCap = System.Drawing.Drawing2D.DashCap.Round;
			tcr.LinePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

			_FontInfoBox.CanExpandGetter = (object o) => {
				var f = o as XmlElement;
				return f != null && f.Name == Constants.Font.ThisName && f.HasChildNodes;
			};
			_FontInfoBox.ChildrenGetter = (object o) =>
			{
				var f = o as XmlElement;
				if (f == null) {
					return null;
				}
				return f.SelectNodes (Constants.Font.Size);
			};
			_FontInfoBox.RowFormatter = (BrightIdeasSoftware.OLVListItem o) =>
			{
				if (_FontInfoBox.GetParent (o.RowObject) == null) {
					o.SubItems[0].Font = new Font (o.SubItems[0].Font, FontStyle.Bold);
					o.SubItems[1].Text = String.Empty;
					o.BackColor = Color.LightBlue;
				}
			};
			_FontNameSizeColumn.AspectGetter = (object o) =>
			{
				var f = o as XmlElement;
				if (f == null) {
					return null;
				}
				if (f.Name == Constants.Font.ThisName) {
					return f.GetAttribute (Constants.Font.Name);
				}
				else if (f.ParentNode?.Name == Constants.Font.ThisName) {
					f.GetAttribute(Constants.Font.Size).TryParse(out float p);
					var t = f.GetAttribute (Constants.FontOccurance.FirstText);
					return String.Concat (p.ToText(), "(", t, ")");
				}
				return null;
			};
			_CountColumn.AspectGetter = (object o) => {
				if (o is XmlElement f) {
					f.GetAttribute(Constants.FontOccurance.Count).TryParse(out int p);
					return p;
				}
				return null;
			};
			_FirstPageColumn.AspectGetter = (object o) => {
				if (o is XmlElement f) {
					f.GetAttribute(Constants.FontOccurance.FirstPage).TryParse(out int p);
					return p;
				}
				return null;
			};
			_ConditionColumn.AspectGetter = (object o) => o is AutoBookmarkCondition c ? c.Description : (object)null;
		}

		private void FontFilterForm_Load (object sender, EventArgs e) {
			if (_fontInfo == null) {
				FormHelper.ErrorBox ("缺少字体信息。");
				_OkButton.Enabled = false;
				return;
			}

			var fonts = _fontInfo.SelectNodes (Constants.Font.ThisName + "[@" + Constants.Font.Name + " and " + Constants.Font.Size + "]");
			var fi = new XmlElement[fonts.Count];
			var i = 0;
			foreach (XmlElement f in fonts) {
				fi[i++] = f;
			}
			_FontInfoBox.AddObjects (fi);
			foreach (XmlElement item in _FontInfoBox.Roots) {
				_FontInfoBox.Expand (item);
			}
			_FontInfoBox.EnsureVisible (0);
			_FontInfoBox.Sort (_CountColumn, SortOrder.Descending);
		}

		protected void _OkButton_Click (Object source, EventArgs args) {
			DialogResult = DialogResult.OK;
			if (_FilterBox.Items.Count > 0) {
				FilterConditions = new AutoBookmarkCondition[_FilterBox.Items.Count];
				for (int i = 0; i < FilterConditions.Length; i++) {
					FilterConditions[i] = _FilterBox.GetModelObject (i) as AutoBookmarkCondition;
				}
			}
			Close ();
		}

		protected void _CancelButton_Click (Object source, EventArgs args) {
			DialogResult = DialogResult.Cancel;
			Close ();
		}

		private void _AddFilterMenu_Opening (object sender, CancelEventArgs e) {
			if (_FontInfoBox.FocusedItem == null) {
				if (_FontInfoBox.SelectedItem != null) {
					_FontInfoBox.FocusedItem = _FontInfoBox.SelectedItem;
				}
				else {
					e.Cancel = true;
					return;
				}
			}
			var f = _FontInfoBox.GetModelObject (_FontInfoBox.FocusedItem.Index) as XmlElement;
			if (f == null) {
				e.Cancel = true;
				return;
			}
			var n = (f.ParentNode.Name == Constants.Font.ThisName ? (f.ParentNode as XmlElement) : f).GetAttribute (Constants.Font.Name);
			if (String.IsNullOrEmpty (n)) {
				e.Cancel = true;
				return;
			}
			f.GetAttribute(Constants.Font.Size).TryParse(out float s);

			_AddFilterMenu.Items.Clear ();
			var p = n.IndexOf ('+');
			var m = n.IndexOfAny (new char[] {'-', ','}, p != -1 ? p : 0);
			string fn;
			if (p != -1) {
				if (m > p + 1) {
					fn = n.Substring (p + 1, m - p - 1);
					if (s > 0) {
						_AddFilterMenu.Items.Add ("筛选名称包含“" + fn + "”且尺寸为" + s.ToText() + "的字体").Tag = new FilterSetting (fn, false, s);
					}
					else {
						_AddFilterMenu.Items.Add ("筛选名称包含“" + fn + "”的字体").Tag = new FilterSetting (fn, false, 0);
					}
				}
				fn = n.Substring (p + 1);
				if (s > 0) {
					_AddFilterMenu.Items.Add ("筛选名称包含“" + fn + "”且尺寸为" + s.ToText() + "的字体").Tag = new FilterSetting (fn, false, s);
				}
				else {
					_AddFilterMenu.Items.Add ("筛选名称包含“" + fn + "”的字体").Tag = new FilterSetting (fn, false, 0);
				}
			}
			else if (p == -1 && m != -1) {
				fn = n.Substring (0, m);
				if (s > 0) {
					_AddFilterMenu.Items.Add ("筛选名称包含“" + fn + "”且尺寸为" + s.ToText() + "的字体").Tag = new FilterSetting (fn, false, s);
				}
				else {
					_AddFilterMenu.Items.Add ("筛选名称包含“" + fn + "”的字体").Tag = new FilterSetting (fn, false, 0);
				}
			}
			if (_AddFilterMenu.Items.Count > 0) {
				_AddFilterMenu.Items.Add (new ToolStripSeparator ());
			}
			if (s > 0) {
				_AddFilterMenu.Items.Add ("筛选名称为“" + n + "”且尺寸为" + s.ToText() + "的字体").Tag = new FilterSetting (n, true, s);
			}
			else {
				_AddFilterMenu.Items.Add ("筛选名称为“" + n + "”的字体").Tag = new FilterSetting (n, true, 0);
			}
			e.Cancel = false;
		}

		private void _AddFilterMenu_ItemClicked (object sender, ToolStripItemClickedEventArgs e) {
			var f = e.ClickedItem.Tag as FilterSetting;
			if (f == null) {
				return;
			}
			AutoBookmarkCondition fc = new AutoBookmarkCondition.FontNameCondition (f.FontName, f.FullMatch);
			if (f.Size > 0) {
				var m = new AutoBookmarkCondition.MultiCondition(fc);
				m.Conditions.Add (new AutoBookmarkCondition.TextSizeCondition(f.Size));
				fc = m;
			}
			_FilterBox.AddObject (fc);
		}

		private void ControlEvent (object sender, EventArgs e) {
			if (sender == _RemoveConditionButton) {
				_FilterBox.RemoveObjects (_FilterBox.SelectedObjects);
			}
			else if (sender == _AddConditionButton) {
				_AddFilterMenu.Show (_AddConditionButton, 0, _AddConditionButton.Height);
			}
		}
	}
}
