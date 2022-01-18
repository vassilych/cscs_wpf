- zamijena DataContexta s Name-om. ( CSCS_GUI.cs -> CSCS_GUI.AddWidgetActions() )
	-> Name se koristi za SVE osim bindanja
	-> za bindanje varijable (Text u TextBox) koristi se DataContext


	** provjerit, testirat, finiširat...



- dodavanje PRE, CHECK i POST evenata
	-> GotFocus -> moze returnat false -> fokus se vraća od kud je doša
	-> PreviewKeyboardLostFocus -> e.Handled = true; //-> cancela micanje van objekta
	-> LostFocus

	* -> UPDATE: (14.10.2021.) ipak ćemo koristit samo PRE i POST evente
		- PRE: pali se pri ulasku u element -> može vratit true ili false...
		- POST: triggera C# WPF PreviewLostKeyboardFocusEventHandler
			- može vratit true ili false, false cancelira izlazak iz elementa