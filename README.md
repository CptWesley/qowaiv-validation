![Qowaiv](https://github.com/Qowaiv/Qowaiv/blob/master/design/qowaiv-logo_linkedin_100x060.jpg)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Code of Conduct](https://img.shields.io/badge/%E2%9D%A4-code%20of%20conduct-blue.svg?style=flat)](https://github.com/Qowaiv/qowaiv-validation/blob/master/CODE_OF_CONDUCT.md)

| version                                                                       | package                                                                                             |
|-------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
|![v](https://img.shields.io/badge/version-0.3.0-darkblue.svg?cacheSeconds=3600)|[Qowaiv.Validation.Abstractions](https://www.nuget.org/packages/Qowaiv.Validation.Abstractions)      |
|![v](https://img.shields.io/badge/version-1.3.0-blue.svg?cacheSeconds=3600)    |[Qowaiv.Validation.DataAnnotations](https://www.nuget.org/packages/Qowaiv.Validation.DataAnnotations)|
|![v](https://img.shields.io/badge/version-0.3.0-blue.svg?cacheSeconds=3600)    |[Qowaiv.Validation.Fluent](https://www.nuget.org/packages/Qowaiv.Validation.Fluent)                  |
|![v](https://img.shields.io/badge/version-0.3.0-blue.svg?cacheSeconds=3600)    |[Qowaiv.Validation.Guarding](https://www.nuget.org/packages/Qowaiv.Validation.Guarding)              |
|![v](https://img.shields.io/badge/version-0.3.0-blue.svg?cacheSeconds=3600)    |[Qowaiv.Validation.Messages](https://www.nuget.org/packages/Qowaiv.Validation.Messages)              |
|![v](https://img.shields.io/badge/version-0.3.0-blue.svg?cacheSeconds=3600)    |[Qowaiv.Validation.Xml](https://www.nuget.org/packages/Qowaiv.Validation.Xml)                        |
|![v](https://img.shields.io/badge/version-0.3.0-darkred.svg?cacheSeconds=3600) |[Qowaiv.Validation.TestTools](https://www.nuget.org/packages/Qowaiv.TestTools)                       |

# Qowaiv Validation
There are multiple ways to support validation within .NET. Most notable are
* [System.ComponentModel](https://www.nuget.org/packages/System.ComponentModel)
* [FluentValidation.NET](https://fluentvalidation.net)

Qowaiv.Validation aims to provide extensions on top of those that work well when
using [Qowaiv SVO's](https://github.com/Qowaiv/Qowaiv), and pretent vendor lock-in.


## Qowaiv Validation Abstractions
To prevent a vendor lock-in, `Qowaiv.Validation.Abstractions` has been introduced.
To achieve that the following is added:

### IValidator&lt;TModel&gt;
The main interface. Validates a model and returns the validation result.
``` C#
namespace Qowaiv.Validation.Abstractions
{
    public interface IValidator<TModel>
    {
        Result<TModel> Validate(TModel model);
    }
}
```

### Result and Result&lt;TModel&gt;
A (validation) result, containing validation messages. Creation is only via
factory methods.
``` C#
namespace Qowaiv.Validation.Abstractions
{
    public class Result
    {
        public IReadOnlyList<IValidationMessage> Messages { get; }
        public bool IsValid => !Errors.Any();

        public IEnumerable<IValidationMessage> Errors => Messages.GetErrors();
        public IEnumerable<IValidationMessage> Warnings => Messages.GetWarnings();
        public IEnumerable<IValidationMessage> Infos => Messages.GetInfos();

        public static Result OK => new Result(Enumerable.Empty<IValidationMessage>());
        public static Result<T> Null<T>(params IValidationMessage[] messages) => new Result<T>(messages);
        public static Result<T> For<T>(T value, params IValidationMessage[] messages) => new Result<T>(value, messages);
        public static Result WithMessages(params IValidationMessage[] messages) => new Result(messages);
        public static Result<T> WithMessages<T>(params IValidationMessage[] messages) => new Result<T>(default, messages);
    }
}
```

The generic result contains also the value/validated model. This `Value` is
only accessible when the model is valid, otherwise, an `InvalidModelException`
is thrown. This exception contains the validation messages. The result is considered
invalid if the value is `null`, unless explicitly created with `Result.Null<T>()`.

``` C#
namespace Qowaiv.Validation.Abstractions
{
    public sealed class Result<T> : Result
    {
        public T Value => IsValid
            ? _value
            : throw InvalidModelException.For<T>(Errors);
    }
}
```

Typical use cases are:

``` C#
Result<DataType> result = Result.For(value);
Result<DataType> resultWithMessages = Result.For(value, messages);
Task<Result<DataType>> asyncResult = Result.For(value).AsTask();
```

#### Composed Actions
A Composed Action can be created by *method chaining* of multiple smaller
actions/functions. Subsequent actions are executed while the `Result<TModel>`
is valid:

``` C#
Result<DataType> result = GetModel()
    .Act(m => m.Action1())
    .Act(m => m.Action2());
```

Or with the `|` pipe operator:
``` C#
Result<DataType> result = GetModel()
    | (m => m.Action1())
    | (m => m.Action2());
```


This is short for:
``` C#
Result<DataType> result = GetModel()
if (result.Isvalid)
{
    result = result.Value.Action1();
}
if (result.Isvalid)
{
    result = result.Value.Action2();
}
```

It is also possible to have multiple acts that update a shared context:
``` C#
Result<Context> context = NewContext()
    .Act(c => Service.GetValue(), (c, value) => c.Value = value)
    .Act(c => Service.GetOther(), (c, other) => c.Value = other);
```
Or, with a (shared) immutable context:
``` C#
Result<Context> context = NewContext()
    .Act(c => Service.GetValue(), (c, value) => /* return Context */ c.Update(value))
    .Act(c => Service.GetOther(), (c, other) => /* return Context */ c.Update(other));
```

#### Casting
The following castings are supported:
``` C#
Result<T> implicit = new T();
T explicit = Result.For<T>(new T());
Result<TOut> casted = Result.For<T>(new T()).Cast<TOut>();
```
The explicit casts fails if the result was not valid. The `Cast<TOut>()` fails
when `TOut` is not a subclass of `T`.

### IValidationMessage
The common ground of validation messages.
``` C#
namespace Qowaiv.Validation.Abstractions
{
    public interface IValidationMessage
    {
        ValidationSeverity Severity { get; }
        string PropertyName { get; }
        string Message { get; }
    }
}
```

There are implementations available in `Qowaiv.Validation.Abstraction`,
`Qowaiv.Validation.Fluent` and `Qowaiv.Validation.DataAnnotation`. You can pick
your implementation of choice based on your scenario. 

#### AccessDenied
`Qowaiv.Validation.Messages` contains a specific implementation of `IValidationMessage`
for communicating insufficient rights. A use case for this can be to
communicate a `403 - Forbidden` response.

#### ConcurrencyIssue
`Qowaiv.Validation.Messages` contains a specific implementation of `IValidationMessage`
for communicating that a concurrency issue. A use case for this can be to
communicate a `409 - Conflict` response.

#### EntityNotFound
`Qowaiv.Validation.Messages` contains a specific implementation of `IValidationMessage`
for communicating that an entity could not be found. A use case for this can be to
communicate a `404 - Not Found` response.

#### ServiceUnavailable
`Qowaiv.Validation.Messages` contains a specific implementation of `IValidationMessage`
for communicating that a service was unavailable. A use case for this can be to
communicate a `503 - Service Unavailable` response.

## Qowaiv exensions on [*Fluent Validation](https://fluentvalidation.net/)


### Validators

#### Not unknown
The `UnknownValidation` validates that a value does not equal the Unknown
value (if existing of course). Accessible via the fluent syntax.

``` C#
public class CustomValidator : AbstractValidator<Model>
{
    public CustomValidator()
    {
        RuleFor(m => m.Email).NotEmptyOrUnknown();
        RuleFor(m => m.Iban).NotUnknown();
    }
}
```

#### Required
The `RequiredValidation` validates that a required property has a set value. If
specified, an unknown value can be seen as a set value, by default it is not.

``` C#
public class CustomValidator : AbstractValidator<Model>
{
    public CustomValidator()
    {
        RuleFor(m => m.Email).Required();
        RuleFor(m => m.Iban).Required(allowUnknown: true);
    }
}
```

### relative to the clock

The `ClockValidation` validates if a date (time) is in the past, or future.
It supports `Date`, `DateTime`, `Date?`, and `DateTime?`, and the provision
of custom date (time) provider. By Default, `Clock.Now()` and `Clock.Today()`
are used.

``` C#
public class CustomValidator : AbstractValidator<Model>
{
    public CustomValidator()
    {
        RuleFor(m => m.Date1).InFuture();
        RuleFor(m => m.Date2).InPast();
        RuleFor(m => m.Date3).NotInFuture();
        RuleFor(m => m.Date4).NotInPast(() => CustomeDateProvider());
    }
}
```

#### Email address should be IP-based
The `EmailAddressValidation` validates that an `EmailAddress`
does not have an IP-based domain.

``` C#
public class CustomValidator : AbstractValidator<Model>
{
    public CustomValidator()
    {
        RuleFor(m => m.Email).NotIPBased();
    }
}
```

#### PostalCode valid for specific country
The `PostalCodeValidation` validates that a `PostalCode` value is valid for
a specific `Country`, both static and via another property.

``` C#
public class CustomValidator : AbstractValidator<Model>
{
    public CustomValidator()
    {
        RuleFor(m => m.PostalCode).ValidFor(m => m.Country);
    }
}
```

## Qowaiv DataAnnotation validation


### Validation messages
The difference with Microsoft's default ValidationResult and ValidationMessages is that in this PR we support a severity: info, warning, or error.

Those messages can be created via factory methods:
``` C#
var none = ValidationMessage.None;
var info = ValidationMessage.Info(message, args);
var warn = ValidationMessage.Warning(message, args);
var error = ValidationMessage.Error(message, args);
```

### Validation attributes

#### Mandatory attribute
The `RequiredAttribute` does not work for value types. The `MandatoryAttribute`
does. The default value of the `struct` is not valid. It also is not valid for
the Unknown value, unless that is explicitly allowed.

``` C#
public class Model
{
    [Mandatory(AllowUnknownValue = true)]
    public EmailAddress Email { get; set; }

    [Mandatory()]
    public string SomeString { get; set; }
}
```

#### Any attribute
The `RequiredAttribute` does not work for collections. The `AnyAttribute`
does. It is only valid as he collection contains at least one item.

``` C#
public class Model
{
    [Any()]
    public List<int> Numbers { get; set; }
}
```

#### AllowedValues attribute
The `AllowedValuesAttribute` allows to define a subset of allowed values. It
supports type converters to get the allowed values based on a string value.

``` C#
public class Model
{
    [AllowedValues("DE", "FR", "GB")]
    public Country CountryOfBirth { get; set; }
}
```

#### ForbiddenValues attribute
The `ForbiddenValuesAttribute` allows to define a subset of forbidden values. It
supports type converters to get the allowed values based on a string value.

``` C#
public class Model
{
    [ForbiddenValues("US", "IR")]
    public Country CountryOfBirth { get; set; }
}
```

#### DefinedEnumValuesOnly attribute
The `DefinedEnumValuesOnlyAttribute` limits the allowed values to defined
enums only. By default it supports all possible combinations of defined enums 
when dealing with flags, but that can be restricted by setting 
`OnlyAllowDefinedFlagsCombinations` to true.

``` C#
public class Model
{
    [DefinedEnumValuesOnly(OnlyAllowDefinedFlagsCombinations = false)]
    public SomeEnum CountryOfBirth { get; set; }
}
```

#### DistinctValues attribute
The `DistinctValuesAttribute` validates that all items of the collection are
distinct. If needed, a custom `IEqualityComparer` comparer can be defined.

``` C#
public class Model
{
    [DistinctValues(typeof(CustomEqualityComparer))]
    public IEnumerable<int> Numbers { get; set; }
}
```

#### NestedModel attribute
The `AnnotatedModelValidator` of this packages support nested validation.

``` C#
public class NestedModelWithChildren
{
    public ChildModel[] Children { get; set; }

    public class ChildModel
    {
        [Mandatory()]
        public string Name { get; set; }
    }
}
```

## XML validation
Valdating XML documents via XSD schemas is a common scenario. To benefit from
`Result<T>` the following scenario is supported:

``` C#
var document = XDocument.Parse("<some xml />");
Result<XDocument> result = document.Validate(schema);
```
Where the schema can be `System.IO.Stream`, `System.Xml.Schema.XmlSchema`, or 
`System.Xml.Schema.XmlSchemaSet`.

A schema can also be the source of a model validator:

``` C#
var validator = new SchemaValidator<MyModel>(schema);
Result<MyModel> result = validator.Validate(model);
// or
Result<MyModel> deserialized = validator.Deserialize(stream);
```

So validation can be triggered on an existing model, or when deserializing.

Note that TModel (obviously) has to be XML serializable.

## Guarding
To guard pre-conditions, the fluent syntax `.Must()` guards conditions using a
`Result<T>` to communicate the outcome. So:

``` C#
game.Must().Be(game.Phase == GamePhase.Started, "Game has started");
```

will return a valid `Result<Game>` if the game is the required state,
otherwise an invalid `Result<Game>` with the specified error message
is returned.

Out-of-the-box, `Be`, `NotBe`, and `Exist` are provided, but it can easily be
extended by writing custom extension methods on `Must<TSubject>` based on
what guarding your (domain) logic requires.

## Test Tools
Qowaiv.Valdation comes with a separate [Test Tools package](https://www.nuget.org/packages/Qowaiv.TestTools).
Details about that package can be found [here](src/Qowaiv.Validation.TestTools/README.md).
