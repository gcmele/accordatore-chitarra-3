# 🎸 Accordatore Chitarra - Web Version

**Accordatore per chitarra professionale accessibile direttamente dal browser**

Versione web dell'accordatore che funziona su iPhone Safari, Chrome, Firefox, Edge e altri browser moderni.

---

## 🌐 Accesso Online

Una volta deployato su GitHub Pages, l'accordatore sarà accessibile all'indirizzo:

**https://gcmele.github.io/accordatore-chitarra-3/**

---

## ✨ Caratteristiche

✅ **Algoritmo di autocorrelazione** - Rilevamento preciso della frequenza (±5 cents)  
✅ **Compatibile con iOS Safari** - Funziona direttamente su iPhone senza App Store  
✅ **Design responsive** - Ottimizzato per smartphone e tablet  
✅ **Indicatore visivo** - Barra colorata per accordatura precisa  
✅ **6 corde standard** - Riferimento chiaro per E, A, D, G, B, e  
✅ **Zero installazione** - Basta aprire il link nel browser  

---

## 📱 Come Usare

1. **Apri il link** nel browser (Safari su iPhone, Chrome su Android/Desktop)
2. **Consenti l'accesso al microfono** quando richiesto
3. **Premi AVVIA** per iniziare l'accordatura
4. **Suona una corda** della chitarra
5. **Osserva**:
   - La nota rilevata (es. "E2")
   - La frequenza in Hz
   - Lo scostamento in cents
   - L'indicatore di accordatura (verde = perfetto)
6. **Regola la corda** finché l'indicatore è al centro e verde

---

## 🎯 Accordatura Standard Chitarra

| Corda | Nota | Frequenza |
|-------|------|-----------|
| 6ª    | E    | 82.41 Hz  |
| 5ª    | A    | 110.00 Hz |
| 4ª    | D    | 146.83 Hz |
| 3ª    | G    | 196.00 Hz |
| 2ª    | B    | 246.94 Hz |
| 1ª    | e    | 329.63 Hz |

---

## 🛠️ Tecnologie

- **Web Audio API** - Cattura audio dal microfono
- **Algoritmo di autocorrelazione** - Rilevamento pitch preciso
- **ScriptProcessorNode** - Compatibilità con iOS Safari
- **HTML5 + CSS3 + JavaScript** - Nessuna dipendenza esterna
- **GitHub Pages** - Hosting gratuito con HTTPS

---

## 🔒 Privacy e Sicurezza

- ✅ Tutto viene elaborato localmente nel browser
- ✅ Nessun dato audio viene inviato a server esterni
- ✅ Accesso al microfono richiede esplicito consenso
- ✅ HTTPS garantito da GitHub Pages

---

## 🌐 Compatibilità Browser

| Browser | Versione | Supporto |
|---------|----------|----------|
| Safari (iOS) | 11+ | ✅ Completo |
| Chrome | 60+ | ✅ Completo |
| Firefox | 55+ | ✅ Completo |
| Edge | 79+ | ✅ Completo |
| Opera | 47+ | ✅ Completo |

**Nota**: Il microfono funziona solo su HTTPS (GitHub Pages lo fornisce automaticamente)

---

## 📁 Struttura File

```
/
├── index.html          # Interfaccia utente principale
├── style.css           # Stili responsive
├── pitch-detector.js   # Algoritmo rilevamento frequenza
├── audio.js            # Gestione cattura audio
├── app.js              # Logica applicazione
└── .github/
    └── workflows/
        └── pages.yml   # Deploy automatico GitHub Pages
```

---

## 🚀 Deploy su GitHub Pages

Il deploy è automatico quando si fa push su `main`:

1. Merge del PR su `main`
2. GitHub Actions esegue il workflow `pages.yml`
3. Il sito viene pubblicato su `https://gcmele.github.io/accordatore-chitarra-3/`
4. Pronto all'uso!

Per verificare lo stato del deploy:
- Vai su **Actions** nel repository GitHub
- Controlla il workflow "Deploy to GitHub Pages"

---

## 🐛 Risoluzione Problemi

### ❌ "Accesso al microfono negato"
**Soluzione:**
- Su iPhone: Impostazioni → Safari → Microfono → Consenti per il sito
- Su Chrome: Clicca sull'icona del lucchetto → Permessi sito → Microfono → Consenti
- Ricarica la pagina

### ❌ "Nessun microfono trovato"
**Soluzione:**
- Verifica che il microfono sia collegato e funzionante
- Controlla le impostazioni del sistema operativo
- Prova a usare un altro browser

### ❌ "Non rileva le note"
**Soluzione:**
- Avvicina il microfono/telefono alla chitarra
- Suona la corda più forte
- Elimina rumori di fondo
- Suona solo una corda alla volta

### ❌ "Errore di sicurezza"
**Soluzione:**
- Assicurati di usare HTTPS (GitHub Pages lo fornisce automaticamente)
- Non usare HTTP normale (il microfono non funzionerà)

---

## 💻 Sviluppo Locale

Per testare localmente:

```bash
# Clona il repository
git clone https://github.com/gcmele/accordatore-chitarra-3.git
cd accordatore-chitarra-3

# Avvia un server web locale
python3 -m http.server 8080

# Apri nel browser
open http://localhost:8080
```

**Nota**: Alcune funzionalità (microfono) richiedono HTTPS. Per test HTTPS locale, usa:

```bash
# Con Node.js e http-server
npx http-server -S -C cert.pem -K key.pem
```

---

## 🎨 Personalizzazione

### Modificare i colori

Modifica le variabili CSS in `style.css`:

```css
:root {
    --primary-color: #4CAF50;      /* Verde accordatura OK */
    --danger-color: #f44336;        /* Rosso stonato */
    --warning-color: #ff9800;       /* Arancione quasi OK */
}
```

### Regolare la sensibilità

Modifica i parametri in `app.js`:

```javascript
const pitchDetector = new PitchDetector(
    44100,  // Sample rate
    70,     // Frequenza minima
    1500,   // Frequenza massima
    0.05    // Soglia (0.0-1.0, più basso = più sensibile)
);
```

---

## 🤝 Contributi

Suggerimenti e miglioramenti sono benvenuti!

Per contribuire:
1. Fork del repository
2. Crea un branch per la tua feature
3. Commit delle modifiche
4. Apri una Pull Request

---

## 📄 Licenza

Progetto personale di **gcmele**.  
Puoi usarlo, modificarlo e distribuirlo liberamente.

---

## 📞 Supporto

Per domande o problemi, apri una **Issue** su GitHub:  
👉 [https://github.com/gcmele/accordatore-chitarra-3/issues](https://github.com/gcmele/accordatore-chitarra-3/issues)

---

## 🎵 Buon Divertimento!

**Happy tuning! 🎸🎶**

---

## 🔗 Link Utili

- **Repository GitHub**: [github.com/gcmele/accordatore-chitarra-3](https://github.com/gcmele/accordatore-chitarra-3)
- **Demo Live**: [gcmele.github.io/accordatore-chitarra-3](https://gcmele.github.io/accordatore-chitarra-3)
- **Documentazione Web Audio API**: [developer.mozilla.org/Web_Audio_API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Audio_API)
