# 🎸 Accordatore Chitarra 3

**Accordatore per chitarra professionale con rilevamento preciso della frequenza**

Applicazione desktop Windows realizzata in C# + WPF con algoritmo di pitch detection basato su autocorrelazione per massima precisione.

---

## 📋 Caratteristiche

✅ **Rilevamento preciso della frequenza** tramite algoritmo di autocorrelazione  
✅ **Interfaccia grafica elegante** con paletta stilizzata della chitarra  
✅ **Indicatore visivo di accordatura** con barra orizzontale colorata  
✅ **Level meter** per controllo del livello audio  
✅ **Selezione microfono** tra tutti i dispositivi audio disponibili  
✅ **Controllo sensibilità** con slider di gain regolabile  
✅ **Supporto notazione italiana e anglosassone** (DO-RE-MI / C-D-E)  
✅ **Visualizzazione scostamento in cents** (precisione < 1 cent)  

---

## 🎯 Accordatura Standard Chitarra

| Corda | Nota (IT) | Nota (EN) | Frequenza |
|-------|-----------|-----------|-----------|}
| 6ª    | MI grave  | E         | 82.41 Hz  |
| 5ª    | LA        | A         | 110.00 Hz |
| 4ª    | RE        | D         | 146.83 Hz |
| 3ª    | SOL       | G         | 196.00 Hz |
| 2ª    | SI        | B         | 246.94 Hz |
| 1ª    | mi acuto  | e         | 329.63 Hz |

---

## 🛠️ Tecnologie Utilizzate

- **Linguaggio:** C# 10 (.NET 6)
- **Framework UI:** WPF (Windows Presentation Foundation)
- **Audio Engine:** NAudio 2.2.1
- **Design:** MaterialDesignThemes 4.9.0
- **Algoritmo:** Autocorrelation + Parabolic Interpolation

---

## 💻 Requisiti di Sistema

- **Sistema Operativo:** Windows 10 o Windows 11
- **.NET Runtime:** .NET 6.0 Desktop Runtime (installato automaticamente)
- **Microfono:** Qualsiasi dispositivo di input audio compatibile Windows

---

## 🚀 Come Usare con Visual Studio 2022

### 1️⃣ Clonare il Repository

Apri **Visual Studio 2022** e:

1. Clicca su **"Clone a repository"** nella schermata iniziale
2. Inserisci l'URL: `https://github.com/gcmele/accordatore-chitarra-3`
3. Scegli una cartella locale dove salvare il progetto
4. Clicca su **"Clone"**

### 2️⃣ Aprire il Progetto

Una volta clonato:

1. Visual Studio aprirà automaticamente la soluzione
2. Nel **Solution Explorer** vedrai il progetto `AccordatoreChitarra`
3. Aspetta che Visual Studio ripristini i pacchetti NuGet (qualche secondo)

### 3️⃣ Compilare ed Eseguire

1. Premi **F5** oppure clicca sul pulsante ▶️ **"Start"** in alto
2. Visual Studio compilerà il progetto e avvierà l'applicazione
3. L'accordatore si aprirà in una finestra

### 4️⃣ Usare l'Accordatore

1. Seleziona il **microfono** dal menu a tendina in basso
2. Regola la **sensibilità** con lo slider se necessario
3. Clicca su **▶ AVVIA** per iniziare
4. Suona una **corda della chitarra**
5. Osserva:
   - La **chiave corrispondente** diventa verde
   - L'**indicatore di accordatura** si sposta
   - La **frequenza** e lo **scostamento in cents** vengono visualizzati
6. Regola la corda finché l'indicatore è al centro (verde)

---

## 📁 Struttura del Progetto

```
AccordatoreChitarra/
│
├── AccordatoreChitarra.csproj    # File di progetto C#
├── App.xaml                       # Configurazione applicazione WPF
├── App.xaml.cs                    # Code-behind applicazione
│
├── MainWindow.xaml                # Interfaccia grafica (UI)
├── MainWindow.xaml.cs             # Logica UI e gestione eventi
│
├── AudioEngine.cs                 # Gestione cattura audio con NAudio
├── PitchDetector.cs               # Algoritmo di rilevamento frequenza
│
└── README.md                      # Questa documentazione
```

---

## 🔧 Architettura Tecnica

### 1. **AudioEngine.cs**
- Gestisce la cattura audio dal microfono tramite `NAudio.WaveInEvent`
- Converte campioni PCM 16-bit in float normalizzati (-1.0 a +1.0)
- Applica gain regolabile e prevenzione del clipping
- Emette eventi con i campioni audio processati

### 2. **PitchDetector.cs**
- Implementa **algoritmo di autocorrelazione** per rilevamento pitch
- **Interpolazione parabolica** per precisione sub-campione
- Calcola **confidence** del rilevamento (0.0 - 1.0)
- Converte frequenze in **note musicali** con offset in cents
- Range ottimizzato per chitarra: 70-1500 Hz

### 3. **MainWindow.xaml / .cs**
- Interfaccia grafica con **paletta chitarra stilizzata**
- 6 chiavi (piroli) che si illuminano quando rilevano la nota corrispondente
- **Indicatore di accordatura** con barra orizzontale:
  - Verde al centro = perfettamente accordato
  - Rosso a sinistra = calante (flat)
  - Rosso a destra = crescente (sharp)
- **Level meter** per monitorare intensità del segnale
- Controlli per selezione microfono e regolazione gain

---

## 🎨 Personalizzazione

### Modificare i Colori

Apri `MainWindow.xaml` e cerca i colori esadecimali:

```xml
<!-- Esempio: cambiare il colore delle chiavi attive -->
<Setter Property="Background" Value="#4CAF50"/>  <!-- Verde -->
```

### Regolare la Sensibilità Algoritmo

Apri `PitchDetector.cs` e modifica i parametri nel costruttore:

```csharp
public PitchDetector(
    sampleRate: 44100,
    minFrequency: 70,      // Frequenza minima rilevabile
    maxFrequency: 1500,    // Frequenza massima rilevabile
    threshold: 0.05f       // Soglia minima segnale (0.0-1.0)
)
```

---

## 🐛 Risoluzione Problemi

### ❌ "Errore nell'avvio dell'accordatore"
**Soluzione:** 
- Verifica che il microfono sia collegato e funzionante
- Controlla le impostazioni audio di Windows
- Prova a selezionare un altro microfono dal menu

### ❌ "Non rileva le note"
**Soluzione:**
- Aumenta la sensibilità con lo slider
- Avvicina il microfono alla chitarra
- Suona la corda più forte
- Verifica che il level meter si muova quando suoni

### ❌ "Rileva la nota sbagliata"
**Soluzione:**
- Assicurati di suonare solo una corda alla volta
- Evita rumori di fondo
- Riduci la sensibilità se l'ambiente è rumoroso

### ❌ "Errore di compilazione: NAudio non trovato"
**Soluzione:**
- Clicca con il tasto destro sulla soluzione → **"Restore NuGet Packages"**
- Oppure: Tools → NuGet Package Manager → Package Manager Console
- Esegui: `Update-Package -Reinstall`

---

## 📦 Creare un Eseguibile (.exe)

### Compilazione Release

1. In Visual Studio, seleziona **"Release"** invece di "Debug" (in alto)
2. Clicca su **Build → Build Solution** (o premi Ctrl+Shift+B)
3. L'eseguibile si troverà in: `bin\Release\net6.0-windows\AccordatoreChitarra.exe`

### Pubblicazione Stand-Alone

Per creare un .exe che funziona anche su PC senza .NET installato:

1. Tasto destro sul progetto → **Publish**
2. Seleziona **Folder** come target
3. Configura:
   - **Target Framework:** net6.0-windows
   - **Deployment Mode:** Self-contained
   - **Target Runtime:** win-x64 (o win-x86 per 32-bit)
4. Clicca **Publish**

Il risultato sarà una cartella con tutto il necessario per eseguire l'app.

---

## 📄 Licenza

Progetto personale di **gcmele**.  
Puoi usarlo, modificarlo e distribuirlo liberamente.

---

## 🤝 Contributi

Suggerimenti e miglioramenti sono benvenuti!  
Apri una **Issue** o una **Pull Request** su GitHub.

---

## 📞 Supporto

Per domande o problemi, apri una **Issue** su GitHub:  
👉 [https://github.com/gcmele/accordatore-chitarra-3/issues](https://github.com/gcmele/accordatore-chitarra-3/issues)

---

## 🎵 Buon Divertimento!

**Happy tuning! 🎸🎶**