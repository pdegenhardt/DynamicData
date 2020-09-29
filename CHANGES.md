# Version 7 Changes

## LINQ Operators
- Deprecate keywords for matching LINQ operators, eg.
  - Transform
  - GroupOn
  - Filter
  
  This is to make it consistent with the LINQ operators users may laready be familiar with.  It avoids confusion around how to approach the API and makes the operators more discoverable.
  
## Deprecate Connect
  - Move to a `ToObservable()` convention
    
  This is to avoid confusion with `IConnectableObservable`.
  
## Single `DataSource`
  - `DataSource<TValue>`
  - `DataSource<TKey, TValue>`
    
  The motivation for this is to have a single set of operators to access data.
