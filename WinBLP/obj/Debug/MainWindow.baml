   M S B A M L   `   `   `  ����  WinBLPdB    WinBLPdB.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5        $	��768��5   -   $
��1026��6:     � Title  6	   ��  �� Path$ windowTitle�.��5      ]� .+�5   
   ��  �� Orientation$ 
Horizontal=�5      $��25��5      $	��Top=�5	      .+�5      o� -   5         o�Name$ MainMenu��5      $	��150��5      .@�5      m� -   5         m�Name$ MainMenuItem��5      $
��File��61   .@�5      m� -   5        $ miOpen��5      $��
Open Files��6/   6C   m� 5      -   5        $ miOpenFolder��5      $��Open Folder��65   6J   m� 5      -   5        $ miEditBatList��5      $��Edit Bat List��66   6M   m� 5      -   5        $ miSortOrder��5      $��Modify File Order��64   {�.  6a   m� 5      -   5        $ 	miProcess��5      $��Process��62   {�.  6U   m� 5      -	   5        $ miSave��5      $
��Save��6/   {�.  6O   m� 5      -
   5        $ miExit��5      $
��Exit��6/   6=   5      m� 5      -   5        $ MainMenuHelp��5      $
��Help��61   .@�5      m� -   5        $ miHelp��5      $
��Help��6/   6=   m� 5      -   5        $ miAbout��5      $��About��60   6?   5      5      �� 5      -   5         ��Name$ OutputLocationLabel��5      $��Stretch=�61   6X   5!      � 5"   
   -   5         �Name$ TextBoxGrid��5"       � ColumnDefinitions 5#      �� 5$      $
��100* �6#   �� 5%      $
��100* �6#   .+�5'      �� -   5         ��Name$ stackPanelScroller��5(      $��0��5)      $��Stretch=�5*      ��. $
��Auto=�5,      $
��Auto=�5-      $��Stretch=�5.      	 �� PanningRatio$		 0.1Q�64   .��50      �� -   5        
 ��Name$
 dpMMultiWindowPanel��52      $ Vertical=�53      $
��Auto=�54      $
��Auto=�55      5:      5;      �� 5<      -   5        $ OutputWindowScrollViewer��5=      $��Stretch=�5>      ��. $��Stretch=�5@      $��1��5A      $
��Auto=�6!   $
��Auto=�6D   .��5C      �� -   5         ��Name$ tbkOutputText��5D      ��. $
��Auto=�5F      $
��Auto=�5G       J� IsManipulationEnabled . |�.  �� AcceptsReturn .  �� 
AcceptsTab .  �� AutoWordSelection . 5L      5O      5P      5R      5S      