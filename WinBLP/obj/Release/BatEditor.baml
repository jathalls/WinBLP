   M S B A M L   `   `   `  ���� R  NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35L5clr-namespace:System.Diagnostics;assembly=WindowsBaseSystem.Diagnostics  J FWinBLPdB, Version=3.0.5735.31153, Culture=neutral, PublicKeyToken=null#clr-namespace:WinBLPdBWinBLPdB    WinBLPdB.BatEditor   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml      @d2http://schemas.microsoft.com/expression/blend/2008      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006      Gdebug5clr-namespace:System.Diagnostics;assembly=WindowsBase      (localclr-namespace:WinBLPdB        � Title$  	BatEditor��$	��600��$
��1000�� � 	Resources ��%    .���  � ColumnDefinitions �� $	��200 ��� $��5 ��� $
��376* � � RowDefinitions �� $	��42* ��� $
��489* ��� $	��40* � � DataContext � -   batData�� � XPath$ /BatLibrary/Bat�� � IsAsynchronous .  ,   &System.Windows.Data.DataSourceProvider   IsInitialLoadEnabled . 1   +System.Diagnostics.PresentationTraceSources   
TraceLevel$
 High=�.+��� -   	 ��Name$	 gridSplitter��$��1��$��3��$��Stretch=�$��Stretch=��� -   
 ��Name$
 ButtonPanel��$��2��$��2�� �� Orientation$ 
Horizontal=�$��Right=�.+��� -    ��Name$ MoveUpRecordButton��$��70��$��20��$��UP��$��5,0,10,0q�$
��Left=��� -   $ MoveDownRecordButton��$��70��$��20��$
��DOWN��$��	5,0,100,0q�$
��Left=��� -   $ AddRecordButton��$��70��$��20��$	��ADD��$��5,0,10,0q�$��Right=��� -   $ DelRecordButton��$��70��$��20��$	��DEL��$��	5,0,100,0q�$��Right=��� -   $ OKButton��$��70��$��20��$��OK��$��5,0,5,0q�$��Right=��� -	   $ CancelButton��$��70��$��20��$��CANCEL��$��5,0,5,0q�$��Right=� �� IsCancel . ]� $��0��$��1��.+��� -
    ��Name$ BatNameTagListBox��$	��Top=��� -    ��Name$ BatNameListBox������ ���� .R��� ����  �� XPath$ @Name���� -    ��Name$ BatEditorGroupBox��$��2��$��1��$��Bat Details�� ��  �� ElementName$ BatNameListBox�� �� Path$ SelectedItem�.���� -   $
 BatDetailsStackPanel��$ Vertical=�.+�� -    �Name$ BatDetailsWrapPanel��.+��� $��23��.��Common Name�� $��23��$
��Bold"�$��20���� -    ��Name$ PrefferredCommonNameLabel��$��23��$
��Bold"����� $ BatCommonName���� $ 
Horizontal=�
�����F�.+��� $��12(�$
��Bold"�
�����F�
������� �� Padding$ 10,2,2,2q�Common Names�� -   $ CommonNameUpButton��$��40��$��20��
�������
����  �$��	100,0,0,0q�$��UP���� -   $ CommonNameDownButton��$��40��$��20��
�������
����  �$��10,0,0,0q�$
��DOWN���� -   $ CommonNameAddButton��$��40��$��20��
�������
����  �$��10,0,0,0q�$	��ADD���� -   $ CommonNameDelButton��$��40��$��20��
�������
����  �$��10,0,0,0q�$	��DEL���� -   $ CommonNameEditBox��$��20��$	��250��$�� ��$��20,0,0,0q��� -    ��Name$ CommonNameListView��$��10,10,10,20q�$��0q����� $ BatCommonName������ .R�� .+��� $��50������ $ 	InnerText��� $��12(�$
��Bold"�
�����F�
�������$ 10,2,2,2q�Genus�� -   $ GenusTextBlock�� �� TextWrapping$
 Wrap=�$��10,10,10,20q����� $ BatGenus�� �� Mode$ TwoWay=��� $��12(�$
��Bold"�
�����F�
�������$ 10,2,2,2q�	Species�� -   $ SpeciesTextBlock��$
 Wrap=�$��10,10,10,20q����� $ 
BatSpecies��$ TwoWay=��� $ 
Horizontal=�
�����F�.+��� $��12(�$
��Bold"�
�����F�
�������$ 10,2,2,2q�	ID Tags�� -   $ TagUpButton��$��40��$��20��
�������
����  �$��	100,0,0,0q�$��UP���� -   $ TagDownButton��$��40��$��20��
�������
����  �$��10,0,0,0q�$
��DOWN���� -   $ TagAddButton��$��40��$��20��
�������
����  �$��10,0,0,0q�$	��ADD���� -   $ TagDelButton��$��40��$��20��
�������
����  �$��10,0,0,0q�$	��DEL���� -   $ IDTagEditBox��$��20��$	��250��$��20,0,0,0q�$�� ���� -   $ BatTagsListView��$��10,10,10,20q�$��0q����� $ BatTag������ .R�� .+��� $��50������ $ 	InnerText�