# ADR 0001: Monorepo και δομή φακέλων

* **Κατάσταση:** Αποδεκτή
* **Ημερομηνία:** 2026-10-31

## Πλαίσιο

Το Evolve αποτελείται από τέσσερα domain services (Members, Classes, Court, Academy), projects υποδομής (AppHost και ServiceDefaults του .NET Aspire, Gateway, Realtime), ένα frontend σε React με TypeScript, test projects, τα πειράματα της σύγκρισης ταυτοχρονισμού και αρχεία ρυθμίσεων υποδομής (π.χ. Keycloak).

Το σύστημα το αναπτύσσει ένας developer. Πολλές αλλαγές αγγίζουν περισσότερα από ένα services ταυτόχρονα: ένα νέο event αλλάζει και αυτόν που το δημοσιεύει και αυτούς που το καταναλώνουν. Επιπλέον, οι κανόνες build (ADR 0004) και οι εκδόσεις των πακέτων (ADR 0005) πρέπει να είναι κοινοί για όλα τα projects.

## Απόφαση

Ένα repository για όλο το σύστημα, με την εξής δομή:

```
platform/                     AppHost, ServiceDefaults, Gateway, Realtime
services/<Service>/<Project>  π.χ. services/Members/Evolve.Members.Domain
web/                          frontend (React με TypeScript)
tests/<Service>/<Project>     π.χ. tests/Members/Evolve.Members.Domain.Tests
experiments/                  πειράματα (σύγκριση ταυτοχρονισμού)
infra/                        ρυθμίσεις υποδομής (π.χ. realm του Keycloak)
docs/adr/                     Architecture Decision Records
```

Κανόνες:

* Τα projects ονομάζονται `Evolve.<Service>.<Layer>` και ο φάκελος έχει το ίδιο όνομα με το project.
* Κάθε service δεν έχει φάκελο `src/`: τα projects του βρίσκονται απευθείας κάτω από `services/<Service>/`.
* Τα tests βρίσκονται στον φάκελο `tests/` της ρίζας και καθρεφτίζουν τη δομή του `services/`.
* Οι solution folders του `Evolve.slnx` καθρεφτίζουν τους φακέλους του δίσκου.
* Κανένα project ενός service δεν αναφέρει (project reference) project άλλου service. Τα services επικοινωνούν μόνο μέσω HTTP ή events.
* Ο φάκελος δηλώνει ποιος επιτρέπεται να εξαρτάται από ποιον. Το `services/` περιέχει μόνο domain services και ο παραπάνω κανόνας ισχύει εκεί χωρίς εξαιρέσεις. Το `platform/` περιέχει ό,τι δεν έχει domain και επιτρέπεται να γνωρίζει πολλά services: το AppHost τα ξεκινά όλα και το Realtime ακούει events από όλα.

## Εναλλακτικές που εξετάστηκαν

* **Ένα repository ανά service (polyrepo):** απορρίφθηκε. Δίνει ανεξαρτησία σε ξεχωριστές ομάδες, που εδώ δεν υπάρχουν. Αντίθετα, μια αλλαγή σε event θα απαιτούσε συντονισμένα commits σε πολλά repositories, οι κοινές ρυθμίσεις build θα επαναλαμβάνονταν σε καθένα και τα κοινά contracts θα έπρεπε να διανέμονται ως πακέτα NuGet με δικό τους versioning.
* **Φάκελος `src/` μέσα σε κάθε service:** απορρίφθηκε. Προσθέτει ένα επίπεδο στα paths χωρίς νέα πληροφορία, αφού ο φάκελος `services/<Service>/` ήδη ομαδοποιεί τα projects του service.
* **Tests μέσα σε κάθε service** (π.χ. `services/Members/Evolve.Members.Domain.Tests`): απορρίφθηκε. Έχει το πλεονέκτημα ότι όλο το service βρίσκεται σε έναν φάκελο. Όμως οι διαφορές των tests θα χρειάζονταν ένα `Directory.Build.props` σε κάθε service, ενώ τώρα αρκεί ένα στο `tests/` (ADR 0004). Επιπλέον, τα integration tests που ξεκινούν πολλά services μαζί δεν ανήκουν σε κανένα service και θα χρειάζονταν ούτως ή άλλως φάκελο εκτός `services/`. Ο διαχωρισμός ανά service διατηρείται μέσα στο `tests/`, που καθρεφτίζει το `services/`.
* **Gateway και Realtime στο `services/`:** απορρίφθηκε. Και τα δύο γίνονται deploy ως αυτόνομα services, όμως το Realtime πρέπει να γνωρίζει τα contracts των events όλων των domain services. Στο `services/` ο κανόνας απομόνωσης θα χρειαζόταν εξαίρεση γι' αυτό, και την ίδια εξαίρεση θα έπρεπε να κωδικοποιεί κάθε architecture test. Το Gateway ρυθμίζεται με routes και δεν εξαρτάται από κώδικα άλλων services. Μπαίνει στο `platform/` για συνέπεια, αφού ούτε αυτό έχει domain.
* **Ένα solution ανά service:** απορρίφθηκε. Με περίπου 20 projects ένα solution παραμένει διαχειρίσιμο. Αν το IDE αργήσει, υπάρχουν τα solution filters (`.slnf`) χωρίς αλλαγή στη δομή.

## Συνέπειες

* **Θετικές:** μια αλλαγή που αγγίζει πολλά services γίνεται σε ένα commit, οι κανόνες build και οι εκδόσεις πακέτων ορίζονται σε ένα σημείο, ένα pipeline CI (Continuous Integration) καλύπτει όλο το σύστημα και τα ADRs βρίσκονται δίπλα στον κώδικα που περιγράφουν.
* **Αρνητικές:** ο κοινός χώρος κάνει εύκολη την παραβίαση των ορίων: τίποτα στο MSBuild δεν εμποδίζει ένα project του Members να αναφέρει project του Classes. Ο κανόνας της ενότητας Απόφαση πρέπει να ελέγχεται, αρχικά στο code review και αργότερα με architecture tests. Επίσης, χωρίς path filters το CI θα κάνει build όλο το σύστημα σε κάθε αλλαγή.
* **Ανοιχτό:** το πού ζουν τα contracts των events που μοιράζονται τα services (κοινό project ή αντίγραφο ανά καταναλωτή) θα αποφασιστεί με το πρώτο event, σε ξεχωριστό ADR.
