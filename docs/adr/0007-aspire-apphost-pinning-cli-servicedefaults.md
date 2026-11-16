# ADR 0007: Υιοθέτηση του .NET Aspire (AppHost, pinning εκδόσεων, CLI, ServiceDefaults)

* **Κατάσταση:** Αποδεκτή
* **Ημερομηνία:** 2026-11-16

## Πλαίσιο

Το Evolve αποτελείται από τέσσερα domain services, το Gateway και το Realtime, και χρειάζεται PostgreSQL, RabbitMQ και Keycloak για να τρέξει. Το .NET Aspire ανήκει στο stack της διπλωματικής: ξεκινά όλα αυτά τοπικά με μία εντολή και δίνει dashboard για κατανεμημένη ιχνηλάτηση με OpenTelemetry. Το ADR αυτό δεν εξετάζει αν θα χρησιμοποιηθεί το Aspire. Καταγράφει **πώς** μπαίνει στο repo.

Η εισαγωγή του αγγίζει τρεις προηγούμενες αποφάσεις:

* **ADR 0003:** το `global.json` καρφώνει το .NET SDK (Software Development Kit).
* **ADR 0004:** warnings ως errors και NuGet audit και για transitive πακέτα.
* **ADR 0005:** CPM (Central Package Management), όπου κανένα `.csproj` δεν περιέχει αριθμό έκδοσης.

Το Aspire φέρνει δικό του MSBuild SDK (`Aspire.AppHost.Sdk`), δικό του CLI (Command Line Interface) και templates που γράφουν εκδόσεις μέσα στα `.csproj`. Χρειάστηκαν επομένως τέσσερις αποφάσεις, που πάρθηκαν μαζί στην αρχή του walking skeleton και γι' αυτό καταγράφονται σε ένα ADR. Αν κάποια από αυτές αλλάξει, νέο ADR θα αντικαταστήσει μόνο το αντίστοιχο τμήμα και εδώ θα προστεθεί παραπομπή.

## Απόφαση

### 1. Έκδοση, μορφή του AppHost και pinning

* **Έκδοση 13.6.5.** Κριτήριο: το τελευταίο minor που έχει τουλάχιστον ένα patch, στο τελευταίο patch του. Η 13.6.5 διορθώνει το CVE-2026-120006 (CVE: Common Vulnerabilities and Exposures, GHSA-f7vh-h5g3-7jrh, elevation of privilege, βαθμός Important), που αγγίζει κάθε προηγούμενη έκδοση. Με παλαιότερη έκδοση το build δεν θα έσπαγε, γιατί τα NU1901 έως NU1904 μένουν warnings (ADR 0004). Η επιλογή έγινε για να μην ξεκινήσει το repo με μόνιμο warning ευπάθειας.
* **AppHost ως project** (`platform/Evolve.AppHost/Evolve.AppHost.csproj`), όχι ως single file. Τα integration tests με `Aspire.Hosting.Testing` και τα πειράματα ταυτοχρονισμού χρειάζονται project reference προς το AppHost.
* **Η έκδοση του `Aspire.AppHost.Sdk` ζει στο `global.json`**, στο τμήμα `msbuild-sdks`. Το `.csproj` γράφει `Sdk="Aspire.AppHost.Sdk"` χωρίς έκδοση. Η τιμή στο `msbuild-sdks` είναι exact, το `rollForward` του ADR 0003 δεν ισχύει εκεί.
* **Κανόνας:** `global.json` για SDKs, `Directory.Packages.props` για πακέτα, κανένα `.csproj` με έκδοση. Η έκδοση ενός MSBuild SDK δεν μπορεί να ζει στο CPM, γιατί το `Directory.Packages.props` εισάγεται μέσα από το SDK, άρα αφού το SDK έχει ήδη επιλεγεί.
* **Το `Aspire.Hosting.AppHost` δεν δηλώνεται πουθενά.** Το SDK προσθέτει μόνο του το `PackageReference` με εύρος `[13.6.5, )`. Το NuGet επιλέγει τη χαμηλότερη έκδοση που ικανοποιεί το εύρος, δηλαδή τη 13.6.5. Η έκδοσή του έχει έτσι μία μόνο πηγή, το `global.json`.
* **Τα υπόλοιπα πακέτα `Aspire.*`** (π.χ. `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.Testing`) θα μπουν στο `Directory.Packages.props` μέσω ιδιότητας `AspireVersion`, με σχόλιο που δείχνει στο `global.json`.

### 2. CLI: bundle και `DnxPinned`

* **`AspireUseCliBundle` σε `true`.** Το AppHost εκτελείται μέσω του Aspire CLI και όχι μέσω των πακέτων NuGet του orchestrator. Με `false` το SDK βγάζει το warning ASPIRE010 και η τεκμηρίωση αναφέρει ότι ο δρόμος μέσω NuGet θα καταργηθεί.
* **`AspireCliInvocationMode` σε `DnxPinned`.** Το `dotnet run` εκτελεί το `aspire.cli` ακριβώς στην έκδοση του SDK, μέσω της εντολής `dnx` του .NET SDK, ανεξάρτητα από το ποιο `aspire` είναι εγκατεστημένο στο μηχάνημα.
* **Κανόνας:** το `dotnet run` ακολουθεί το repo, το `aspire run` ακολουθεί το μηχάνημα.

### 3. ServiceDefaults και κανόνας εξαρτήσεων

Οι κοινές ρυθμίσεις κάθε service (OpenTelemetry, health checks, service discovery, resilience στους `HttpClient`) ζουν σε ένα project, το `platform/Evolve.ServiceDefaults`, που τα services αναφέρουν ως project reference. Το `Extensions.cs` είναι αυτούσιο από το template της 13.6.5.

Τέσσερις κανόνες εξαρτήσεων, ρητά ανοιχτοί σε αναθεώρηση:

1. Μόνο το layer Api ενός service (ADR 0002) αναφέρει το ServiceDefaults. Επανεξέταση στο πρώτο service με βάση.
2. Το ServiceDefaults είναι το μόνο project του `platform/` που επιτρέπεται να αναφέρεται από service. Επανεξέταση αν το Realtime χρειαστεί κάτι κοινό.
3. Το ServiceDefaults δεν αναφέρει κανένα project και δεν περιέχει domain, contracts ή κοινά DTOs (Data Transfer Objects). Επανεξέταση στο πρώτο event.
4. Το AppHost αναφέρει τα Api projects, το Gateway και το Realtime.

### 4. Template και θέση του `aspire.config.json`

Τα `.csproj` γράφτηκαν με το χέρι, με το template της 13.6.5 ως αναφορά (κρατιέται εκτός repo).

| Αρχείο | Σχέση με το template |
|---|---|
| `Evolve.AppHost.csproj` | Χειρόγραφο. Χωρίς έκδοση στο `Sdk`, χωρίς `TargetFramework`, `ImplicitUsings` και `Nullable` (κληρονομούνται από το `Directory.Build.props`, ADR 0004), με προσθήκη του `AspireCliInvocationMode` και νέο `UserSecretsId` |
| `AppHost.cs`, `appsettings.json`, `Properties/launchSettings.json` | Αυτούσια |
| `appsettings.Development.json` | Δεν κρατήθηκε. Οι ρυθμίσεις του υπάρχουν ήδη όλες στο `appsettings.json` |
| `Evolve.ServiceDefaults.csproj` | Χειρόγραφο. Τα επτά `PackageReference` χωρίς `Version` (ADR 0005), χωρίς τις ιδιότητες που κληρονομούνται |
| `Extensions.cs` | Αυτούσιο (ίδιο SHA256) |
| `aspire.config.json` | **Απόκλιση:** ζει στη ρίζα του repo και όχι στον φάκελο του AppHost, με `path` σε `platform/Evolve.AppHost/Evolve.AppHost.csproj` |

Το `aspire.config.json` μπήκε στη ρίζα γιατί το CLI ψάχνει το αρχείο από τον τρέχοντα φάκελο προς τα πάνω. Στη ρίζα το βρίσκει από οποιονδήποτε υποφάκελο του repo.

## Εναλλακτικές που εξετάστηκαν

* **AppHost ως single file:** απορρίφθηκε. Δεν δίνει project στο οποίο να δείξει ένα test project με `Aspire.Hosting.Testing`.
* **Έκδοση του SDK στο attribute του `.csproj` (`Sdk="Aspire.AppHost.Sdk/13.6.5"`, όπως το template):** απορρίφθηκε. Θα ήταν το μόνο `.csproj` του repo με αριθμό έκδοσης. Η συμπεριφορά όταν το attribute και το `global.json` δηλώνουν διαφορετικές εκδόσεις δεν ελέγχθηκε.
* **`PackageVersion` για το `Aspire.Hosting.AppHost` στο CPM:** δεν γίνεται. Το restore αποτυγχάνει με NU1009 σε όλο το solution, γιατί το πακέτο προστίθεται ήδη από το SDK.
* **`AspireUseCliBundle` σε `false`:** απορρίφθηκε λόγω του ASPIRE010 και της επικείμενης κατάργησης. Παραμένει η εφεδρεία αν το `Aspire.Hosting.Testing` δεν συνεργαστεί με το bundle (βλ. Συνέπειες), με τεκμηριωμένο `NoWarn`.
* **Invocation mode `Path` (η προεπιλογή όταν λείπει η ιδιότητα):** απορρίφθηκε. Χρησιμοποιεί το `aspire` του μηχανήματος, με δοκιμή 5 δευτερολέπτων και εφεδρεία σε DNX, οπότε το `dotnet run` θα έτρεχε όποια έκδοση CLI τύχει να είναι εγκατεστημένη.
* **Αντίγραφο του `Extensions.cs` σε κάθε service:** απορρίφθηκε. Τέσσερα ή περισσότερα αντίγραφα που αποκλίνουν με τον χρόνο, και κάθε αλλαγή στο telemetry γίνεται πολλές φορές.
* **ServiceDefaults ως εσωτερικό πακέτο NuGet:** απορρίφθηκε. Απαιτεί feed, versioning και δημοσίευση για κώδικα που ζει στο ίδιο monorepo με τους καταναλωτές του (ADR 0001).
* **Τα `.csproj` όπως τα παράγει το `dotnet new`:** απορρίφθηκε. Γράφουν έκδοση στο `Sdk` και `Version` σε κάθε `PackageReference`, κάτι που συγκρούεται με το ADR 0005, και επαναλαμβάνουν ιδιότητες του `Directory.Build.props`.
* **`aspire.config.json` στον φάκελο του AppHost (όπως το template):** απορρίφθηκε. Από τη ρίζα ή από το `services/` το CLI δεν θα το έβρισκε, θα έκανε αναζήτηση και θα έγραφε δικό του αρχείο στον τρέχοντα φάκελο.

## Συνέπειες

* **Θετικές:** μία εντολή ξεκινά όλο το σύστημα. Η έκδοση του Aspire αλλάζει σε ένα σημείο για το SDK και το `Aspire.Hosting.AppHost`. Το `dotnet run` δίνει το ίδιο CLI σε κάθε μηχάνημα και στο CI (Continuous Integration). Όλα τα services αποκτούν ίδιο telemetry και ίδια health checks από ένα αρχείο.
* **Αρνητικές, έκδοση:** η 13.6.5 ήταν έξι ημερών όταν υιοθετήθηκε. Όταν μπει η ιδιότητα `AspireVersion`, η έκδοση του Aspire θα υπάρχει σε δύο αρχεία που πρέπει να αλλάζουν μαζί, και τίποτα δεν το επιβάλλει πέρα από το σχόλιο.
* **Αρνητικές, όριο 120 δευτερολέπτων:** το target `_AspireCheckCliVersion` τρέχει `dnx --yes aspire.cli@13.6.5 -- --version` με σκληρό όριο 120000 ms, και σε λειτουργία `Dnx` δεν υπάρχει εφεδρεία απευθείας εκκίνησης. Το πρώτο `dotnet run` απέτυχε με «DNX could not restore or run aspire.cli@13.6.5 (exit code -1)», επειδή η πρώτη λήψη πήρε 154 δευτερόλεπτα. Με ζεστή cache ο έλεγχος παίρνει 0,87 δευτερόλεπτα. Κάθε νέο μηχάνημα και το CI χρειάζονται επομένως ένα βήμα προθέρμανσης πριν από το πρώτο `dotnet run`, που τεκμηριώνεται στο README.
* **Αρνητικές, ServiceDefaults:** όλα τα services δένονται με μία εκδοχή του telemetry και αναβαθμίζονται μαζί. Το `Extensions.cs` είναι κώδικας του template που πλέον συντηρείται στο repo: οι βελτιώσεις μελλοντικών templates δεν έρχονται αυτόματα.
* **Ανοιχτά σημεία:**
  * **Retry και idempotency:** ο `AddStandardResilienceHandler` κάνει αυτόματο retry σε κάθε `HttpClient`. Στις σύγχρονες κλήσεις κράτησης προς το Court, ένα retry μετά από timeout μπορεί να ξαναστείλει κράτηση που είχε πετύχει. Το exclusion constraint αποτρέπει τη διπλή κράτηση, αλλά ο καλών βλέπει αποτυχία. Απόφαση στην πρώτη σύγχρονη κλήση.
  * **Health endpoints:** το `MapDefaultEndpoints` εκθέτει τα `/health` και `/alive` μόνο σε Development. Σε περιβάλλον Production δίνουν 404, άρα πριν από το deployment χρειάζεται απόφαση για το πώς εκτίθενται με ασφάλεια.
  * **Tracing:** το template παρακολουθεί μόνο HTTP. Οι πηγές του WolverineFx και του Npgsql προστίθενται μετά το walking skeleton.
  * **`Aspire.Hosting.Testing` σε λειτουργία bundle:** ελέγχεται στο πρώτο service, όπου θα υπάρχει πραγματικό resource. Από αυτό εξαρτάται αν μένει το `AspireUseCliBundle` σε `true`.
  * **Θέση κλήσης των client integrations του Aspire** (Api ή Infrastructure): αποφασίζεται στο πρώτο service με βάση, μαζί με τον κανόνα 1.
* **Επαλήθευση (16/11/2026):**
  * `dotnet build` με 0 warnings και 0 errors, χωρίς `PackageVersion` για το `Aspire.Hosting.AppHost`. Το AppHost φέρνει 72 πακέτα και το NuGet audit δεν βρήκε ευπάθεια.
  * Προσωρινό `PackageVersion` για το `Aspire.Hosting.AppHost`: το restore απέτυχε με NU1009.
  * `dotnet msbuild -pp` στο `Evolve.Members.Domain`: το `Directory.Packages.props` εισάγεται μέσα από το SDK.
  * `dotnet run --project platform\Evolve.AppHost`: dashboard με κενή λίστα resources και καθαρό κλείσιμο. Η έξοδος είναι του Aspire CLI, άρα το `DnxPinned` παραδίδει πράγματι την εκτέλεση στο CLI.
  * `aspire run` από τη ρίζα και από το `services\`: βρίσκει το AppHost χωρίς αναζήτηση. Χωρίς αρχείο στη ρίζα, το CLI έκανε αναζήτηση και έγραψε δικό του `aspire.config.json` στον τρέχοντα φάκελο. Το αρχείο της ρίζας δεν ξαναγράφεται από το CLI (ίδιο SHA256 πριν και μετά).
  * ServiceDefaults: τα επτά νέα `PackageVersion` δεν προκάλεσαν σύγκρουση transitive pinning στο AppHost.
  * Δεν ελέγχθηκε ξεχωριστά η συμπεριφορά του Rider με το AppHost.
