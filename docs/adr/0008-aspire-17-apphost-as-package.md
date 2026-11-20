# ADR 0008: Αναβάθμιση σε Aspire 17.0.0 (AppHost ως κανονικό project με πακέτα)

* **Κατάσταση:** Αποδεκτή
* **Ημερομηνία:** 2026-11-20
* **Αντικαθιστά:** από το [ADR 0007](0007-aspire-apphost-pinning-cli-servicedefaults.md) τα τμήματα 1 και 4, την ιδιότητα `AspireUseCliBundle` του τμήματος 2 και τη διατύπωση του κανόνα 4 του τμήματος 3. Τα υπόλοιπα του 0007 ισχύουν.

## Πλαίσιο

Το ADR 0007 υιοθέτησε την 13.6.5 με κριτήριο «τελευταίο minor με τουλάχιστον ένα patch, στο τελευταίο patch του». Τέσσερις ημέρες μετά, η τελευταία έκδοση είναι η 17.0.0, που αλλάζει το μοντέλο του AppHost:

* Το MSBuild SDK `Aspire.AppHost.Sdk` γίνεται obsolete (warning ASPIRE012 σύμφωνα με τα release notes, που με warnings ως errors, ADR 0004, σπάει το build).
* Το `AddProject<T>` γίνεται deprecated. Τα projects δηλώνονται με μονοπάτι, μέσω του `AddDotnetProject` από το νέο πακέτο `Aspire.Hosting.Dotnet`.
* Τα C# AppHosts απαιτούν το CLI (Command Line Interface) bundle (error ASPIRE013).

Η 17.0.0 **δεν πληροί** το κριτήριο του 0007: είναι έκδοση .0.0 χωρίς κανένα patch, και το πακέτο του CLI είναι υπογεγραμμένο στις 17 Νοεμβρίου 2026. Η αναβάθμιση αποφασίστηκε ρητά παρά το κριτήριο, για δύο λόγους:

1. **Το κόστος μεγαλώνει με τον χρόνο.** Σήμερα το AppHost έχει ένα μόνο resource και η μετάβαση αγγίζει τέσσερα αρχεία. Κάθε service, κάθε `ProjectReference` και κάθε `AddProject<T>` που θα προστεθεί στο παλιό μοντέλο θα πρέπει αργότερα να μεταφερθεί.
2. **Το dashboard της διπλωματικής.** Η 17.0 φέρνει νέο dashboard. Τα screenshots για το κείμενο και την παρουσίαση βγαίνουν από την αρχή με το UI που θα παραδοθεί.

Το κριτήριο του 0007 εξακολουθεί να ισχύει για τις **επόμενες** αναβαθμίσεις: όταν κυκλοφορήσει patch της 17.0, το repo περνά στο τελευταίο patch.

## Απόφαση

### 1. Έκδοση και πηγή της

* **Έκδοση 17.0.0**, γραμμένη **μόνο** στο `Directory.Packages.props`, στην ιδιότητα `AspireVersion`. Κάθε `PackageVersion` πακέτου `Aspire.*` γράφει `Version="$(AspireVersion)"`.
* Το `global.json` **δεν έχει πια** τμήμα `msbuild-sdks`. Ο κανόνας του 0007 («`global.json` για SDKs, `Directory.Packages.props` για πακέτα, κανένα `.csproj` με έκδοση») μένει ίδιος: το Aspire απλώς πέρασε από την πρώτη κατηγορία στη δεύτερη.
* Το εύρημα NU1009 του 0007 δεν ισχύει πια, και το αρνητικό «η έκδοση του Aspire σε δύο αρχεία» εξαλείφεται.

### 2. AppHost

* `Sdk="Microsoft.NET.Sdk"` και δύο `PackageReference`, στα `Aspire.Hosting.AppHost` και `Aspire.Hosting.Dotnet`, χωρίς `Version` (ADR 0005). Το `IsAspireHost` το θέτει το `build/Aspire.Hosting.AppHost.props` του πακέτου.
* **Χωρίς `ProjectReference`** προς τα services. Κάθε service δηλώνεται στο `AppHost.cs` με:
  * σχετικό μονοπάτι από τον φάκελο του AppHost (έτσι το ερμηνεύει το `AddDotnetProject`, σύμφωνα με την τεκμηρίωση του πακέτου),
  * **ρητό αρχείο `.csproj`** και όχι φάκελο,
  * **`/`** ως διαχωριστικό, ώστε το μονοπάτι να λύνεται και σε Linux.
* Ο **κανόνας 4** του τμήματος 3 του 0007 διατυπώνεται πλέον: «Το AppHost δηλώνει με μονοπάτι στο `AddDotnetProject` τα Api projects, το Gateway και το Realtime.»

### 3. CLI

* **`AspireUseCliBundle`: αφαιρείται από το `.csproj`.** Στην 17.0 η προεπιλογή είναι `true` για κάθε `.csproj`. Η τιμή `false` σε C# AppHost δίνει ASPIRE013, που δεν γίνεται suppress. Μια ρητή δήλωση θα έμοιαζε με επιλογή, ενώ δεν είναι.
* **`AspireCliInvocationMode` `DnxPinned`: μένει.** Στην 17.0 η pinned έκδοση του CLI είναι η έκδοση του πακέτου `Aspire.Hosting.AppHost` (`_AspireAppHostPackageVersion` στα targets), άρα η `AspireVersion` ορίζει **και** το AppHost **και** το CLI που τρέχει το `dotnet run`. Ο κανόνας «το `dotnet run` ακολουθεί το repo, το `aspire run` ακολουθεί το μηχάνημα» ισχύει.

### 4. OpenTelemetry 1.18.0 στο ίδιο commit

Το `Aspire.Hosting.AppHost` 17.0.0 εξαρτάται από τα `OpenTelemetry.Exporter.OpenTelemetryProtocol` και `OpenTelemetry.Extensions.Hosting` στην 1.18.0. Με transitive pinning (ADR 0005), τα `PackageVersion` 1.17.0 θα επέβαλλαν downgrade στο AppHost (αναμενόμενο NU1109, δεν δοκιμάστηκε σκόπιμα). Γι' αυτό και τα πέντε πακέτα `OpenTelemetry.*` του ServiceDefaults πήγαν στην 1.18.0 στο ίδιο commit. Δεν παίρνουν την `AspireVersion`, γιατί δεν είναι μέρος του Aspire και η έκδοσή τους δεν θα ακολουθεί πάντα τη δική του.

### 5. Template

Το template της 17.0.0 παράχθηκε δίπλα σε αυτό της 13.6.5 (εκτός repo) και συγκρίθηκε:

| Αρχείο | Διαφορά 13.6.5 → 17.0.0 | Στο repo |
|---|---|---|
| `Evolve.AppHost.csproj` | `Microsoft.NET.Sdk`, δύο `PackageReference`, χωρίς `AspireUseCliBundle` | Όπως το template, χωρίς `Version` και με `DnxPinned` |
| `AppHost.cs`, `appsettings*.json`, `aspire.config.json` | Καμία | Το `AppHost.cs` αλλάζει μόνο λόγω `AddDotnetProject` |
| `Properties/launchSettings.json` | Μόνο οι τυχαίες θύρες | Αμετάβλητο |
| `Evolve.ServiceDefaults.csproj` | OpenTelemetry 1.17.0 → 1.18.0 | Μέσω CPM |
| `Extensions.cs` | Καμία | Αμετάβλητο |

## Εναλλακτικές που εξετάστηκαν

* **Παραμονή στην 13.6.5 μέχρι το πρώτο patch της 17.0:** απορρίφθηκε για τους λόγους του Πλαισίου. Είναι η επιλογή που θα έδινε το κριτήριο του 0007, και το κόστος της απόρριψης καταγράφεται στις Συνέπειες.
* **Μετάβαση στην 17.0 κρατώντας το `Aspire.AppHost.Sdk`:** απορρίφθηκε. Το ASPIRE012 θα έσπαγε το build λόγω του ADR 0004, και με `NoWarn` θα κρατούσαμε ένα μοντέλο που καταργείται.
* **`AddProject<T>` με `ProjectReference`:** απορρίφθηκε ως deprecated. Το κόστος του `AddDotnetProject` είναι ότι χάνεται ο έλεγχος στο compile: ένα λάθος μονοπάτι φαίνεται στην εκκίνηση του AppHost και όχι στο build.
* **Μονοπάτι σε φάκελο αντί για `.csproj`:** απορρίφθηκε. Σπάει μόλις βρεθεί δεύτερο project στον ίδιο φάκελο, και ο αναγνώστης δεν βλέπει τι ακριβώς τρέχει.
* **`\` στο μονοπάτι:** απορρίφθηκε. Σε αντίθεση με το MSBuild, ένα string σε C# περνά αυτούσιο, και σε Linux (CI, container) η `\` είναι χαρακτήρας ονόματος αρχείου.
* **Ρητό `AspireUseCliBundle` `true`:** απορρίφθηκε. Η ιδιότητα δεν μπορεί πια να πάρει άλλη τιμή.
* **Αριθμός έκδοσης σε κάθε `PackageVersion` αντί για `AspireVersion`:** απορρίφθηκε. Τα πακέτα `Aspire.*` αναβαθμίζονται πάντα μαζί, και σύντομα θα προστεθούν τα `Aspire.Hosting.PostgreSQL` και `Aspire.Hosting.Testing`.
* **OpenTelemetry σε χωριστό, μεταγενέστερο commit:** δεν γίνεται, λόγω transitive pinning (τμήμα 4).
* **Αλλαγές σε τρία commits σε χωριστό branch:** απορρίφθηκε. Τα ενδιάμεσα commits δεν θα έχτιζαν (χωρίς `msbuild-sdks` το παλιό `.csproj` δεν κάνει restore, χωρίς `ProjectReference` η κλάση `Projects.*` δεν υπάρχει). Ένα commit κρατά το `main` να χτίζει σε κάθε σημείο, ώστε το `git bisect` να μένει χρήσιμο.

## Συνέπειες

* **Θετικές:**
  * Μία πηγή για την έκδοση του Aspire, η `AspireVersion`, που ορίζει και το CLI του `dotnet run`.
  * Το build του AppHost δεν εξαρτάται πια από τα services: στο `dotnet build` χτίζεται παράλληλα με αυτά.
  * Το νέο dashboard χρησιμοποιείται από την αρχή στα screenshots της διπλωματικής.
* **Αρνητικές:**
  * Έκδοση .0.0 χωρίς patch, παρά το κριτήριο του 0007.
  * Λάθος μονοπάτι στο `AddDotnetProject` φαίνεται μόνο στην εκκίνηση.
  * `dotnet build` **μόνο του AppHost** δεν χτίζει τα services, άρα δεν πιάνει compile errors τους. Το `dotnet build` στη ρίζα (`Evolve.slnx`) τα πιάνει. Τα services τα χτίζει το Aspire όταν τα ξεκινά.
  * **Το CLI χωρίστηκε σε δύο πακέτα:** `aspire.cli` (πακέτο `DotnetTool` 48 KB που δείχνει ανά πλατφόρμα) και `aspire.cli.<RID>` (RID: Runtime Identifier, π.χ. `aspire.cli.win-x64`) με το εκτελέσιμο. Η cache της προθέρμανσης είναι επομένως ανά πλατφόρμα: ένας Linux runner του CI κατεβάζει το `aspire.cli.linux-x64`.
  * **Το όριο των 120 δευτερολέπτων του 0007 παραμένει** (`_AspireCliVersionTimeout` 120000 σε λειτουργία Dnx). Η πρώτη λήψη της 17.0.0 πήρε περίπου 3 λεπτά, άρα η προθέρμανση του README και του CI μένει απαραίτητη.
  * Το νέο dashboard διαφέρει από αυτό της 13.6.5: η προβολή γράφου βρίσκεται πλέον μέσα στο Overview. Τα screenshots της 13.6.5 δεν αντιστοιχούν στο UI που παραδίδεται.
* **Ανοιχτά σημεία:**
  * **`Aspire.Hosting.Testing` με bundle:** η εφεδρεία του 0007 (`AspireUseCliBundle` `false` με `NoWarn` για το ASPIRE010) **δεν υπάρχει πια** στο C# AppHost. Αν το test με `Aspire.Hosting.Testing` δεν συνεργαστεί με το bundle στο πρώτο service με βάση, χρειάζεται νέα απόφαση.
  * **Patch της 17.0:** υιοθετείται μόλις κυκλοφορήσει, με το κριτήριο του 0007.
* **Επαλήθευση (20/11/2026):**
  * Aspire CLI 17.0.0 (`17.0.0+2d325b13709752ba6d9a943635c5e2940a2530c0`) και `Aspire.ProjectTemplates` 17.0.0. Η σύγκριση των templates έδωσε τον πίνακα του τμήματος 5.
  * Στο `Aspire.Hosting.AppHost.props` του πακέτου: `AspireUseCliBundle` με προεπιλογή `true` για `.csproj` και `IsAspireHost` `true`. Στο `.targets`: ASPIRE013, `DnxPinned` με την έκδοση του πακέτου, όριο 120000 ms σε Dnx.
  * Στο nuspec του `Aspire.Hosting.AppHost` 17.0.0: εξάρτηση από OpenTelemetry 1.18.0.
  * Προθέρμανση με `dnx --yes aspire.cli@17.0.0 -- --version`: περίπου 3 λεπτά, δημιουργία των `aspire.cli` και `aspire.cli.win-x64` στο NuGet cache.
  * `dotnet build` στη ρίζα: 0 warnings και 0 errors, χωρίς ASPIRE012, ASPIRE013, NU1109 ή ευπάθειες από το NuGet audit.
  * `dotnet clean` του Members Api και στη συνέχεια `dotnet run --project platform\Evolve.AppHost`: το `Evolve.Members.Api.dll` ξαναδημιουργήθηκε 13 δευτερόλεπτα μετά την εκκίνηση. Το Aspire χτίζει μόνο του το project του `AddDotnetProject`.
  * `aspire run` από τη ρίζα με το CLI του μηχανήματος (17.0.0): βρήκε το AppHost από το `aspire.config.json` (`platform\Evolve.AppHost\Evolve.AppHost.csproj`), dashboard στη θύρα 17039, `members-api` ξεκίνησε με HTTP endpoint.
  * Dashboard (θύρα 17039, από το `launchSettings.json`): `members-api` σε Running, trace `members-api: GET /` με ένα Server span.
  * Commit της αλλαγής: `f871735`.
