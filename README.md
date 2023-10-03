# Orleans.UrlShortner

Progetto per la realizzazione di un applicativo UrlShortener utilizando Microsoft Orleans.

## COntenuto del repository

### Slides
la cartella `Slides` contiene la presentazione della sessione.

### Branches
Di seguito una breve spiegazione del contnuto dei branches:

- `1_grains_and_tests`: startup di un'applicazione basata su Microsoft Orleans, come testarla e come migliorarne le performance con gli attributi `OneWay` e `ReadOnly`
- `2_log_dashboard_and_metrics`: aggiunta dei log, della dashboard e del tracing su Zipkin per garantire l'osservabilità dell'applicazione
- `3_reminders_and_stateless_workers`: esploriamo i service grain (stateless worker) e come schedulare attività con Timers e Reminder
- `4_incoming_and_outgoing_filters`: aggiunta dei filtri sia a livello di grain che a livello di silos
- `5_observers`: si vedrà come sottoscrivere notifiche provenienti da altri grain tramite l'interfaccia `IObservable`
- `6_persistence`: aggiungiamo la persistenza dello stato dei grains
- `7_production_ready`: passiamo da un'applicazione in memory, tipida dell'ambiente di sviluppo, ad un'applicazione production-ready
- `8_external_clients`: passaggio da un'architettura `co-hosted clients` ad una `external clients`

