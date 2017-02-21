## Binding system design. ##

### Rationale for yet another data binding library ###

Existing binding solutions

1. Does not work well when used in code.
2. Heavily rely on prebuilt functionality, almost closed to extension. 
3. Verbose due to value converters and validation rules.

Real world example - I need to update an object property only if control property didn't change in 3 seconds.
Conventional binding system solution - add special configuration property 'Delay'
http:www.jonathanantoine.com/2011/09/21/wpf-4-5-part-4-the-new-bindings-delay-property/
THIS IS CRAZY - how I supposed to deal with this without this magic property?
I'm pretty sure there is a workaround (See http://paulstovell.com/blog/wpf-delaybinding) 
- but if you add new use cases by extending your library class interface 
you probably doing something wrong. Such universal thing as data bindings should be completely orthogonal
to everything else and as such it should be extendable by users without changing library interfaces.

'configuration over code' mantra leads to what I call 'non-linear' design. These are designs where
small change in requirements lead to huge changes in code.
For example: simple requirement - move data from string MyData.Property to MyControl.Text 
linear solution: 

```
MyControl.Text = MyData.Property
```

non-linear solution: 

```
Text="{Binding Property}" 
```

Small change in requirement - Property is not a string
linear solution, small change in requirements = small change in code:

```
MyControl.Text = MyData.Property.ToString();
```

non-linear solution: 

```
  <Window.Resources>
    <l:IntToStringConverter x:Key="converter" />
  </Window.Resources>
```

```
Text="{Binding Month, Converter={StaticResource converter}}"
```

```
  public class IntToStringConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return null;
    }
  }
```

I want data bindings to be orthogonal to everything else.

### Sample code ###

```
void ViewDidLoad()
{
//Below snippet connect credentails.PhoneNumber to View.PhoneNumber.Text property
//Text field text is set via custom method SetPhoneNumber(string text) instead of property setter.
//After PhoneNumber (source) updated it colors text field according to property validation status
	Bindings.Property (credentials, _ => _.PhoneNumber)  
		.To (View.PhoneNumber.TextProperty (true))       
		.UpdateTarget ((t, s) => View.Username.SetPhoneNumber (s.Value))
		.AfterSourceUpdate (MeetingsSkin.ValidTextField);

//Simple binding of credentails.CountryCode to CountryCode.Text property.
	Bindings.Property (credentials, _ => _.CountryCode)
		.To (View.CountryCode.TextProperty (false));

//Bind credentails.Action property which could be CredentialsAction.Login or CredentialsAction.Signup
//to UISegmentedControl.SelectedSegmentIndex property
//Before performing binding convert source property to int and vice versa via supplied converter.
	Bindings.Property (credentials, _ => _.Action)
		.Convert<int>(
			_ => _ == CredentialsAction.Login ? LoginView.LoginSegment : LoginView.SignUpSegment,
			_ => _ == LoginView.LoginSegment ? CredentialsAction.Login : CredentialsAction.Signup)
		.To (View.LoginSignUpSelector.SelectedSegmentProperty ());

//Bind credentials.ActionCommand to UIButton TouchUpInside event
//Execute custom code when command started and finished - this code start and stop activity indication and perform few other things.
	Bindings.Command (credentials.ActionCommand)
		.WhenStarted (ActionCommand_Started)
		.WhenFinished (ActionCommand_Finished)
		.To (View.Action.ClickTarget ());

//Bind model.OpenEventDetailsCommand to ListView ItemClick event
//Position is extracted from ItemClickEventArgs when an item is clicked and is passed to command action Parameter
	Bindings.Command(model.OpenEventDetailsCommand)
		.To(view.EventList.ItemClickTarget());

}
```

### App architecture with bindings ###

```
|======================================== View ========================================|
1. Expose 'connection points' as bindable properties and event sources.
|===================================== Controller =====================================|
2. Connect these 'connection points' to model, perform all of the bookkeeping .
|======================================= Model ========================================|
3.Expose own 'connection points' which could be connected to what is exposed by view.
3.1 Implement logic and expose it as ICommand.
3.2 Store data which is exposed as bindable properties.
|================================= Services and Data ==================================|
4. Implement low level data access functions and logic.
```

Assumption here is that most of the time user interacts with application logic by following simple sequence:

1. Setup parameters for a command by typing text, scrolling or selecting something, etc. 
It could be something very simple  like clicking a button or complex like filling large 
form with text, dates, colors, etc.
1. Invoke command and wait until app process request and display result.

For instance:

1. Fill in login and password text fields.
1. Hit login button to authenticate in app.

Or

1. Fill event title and date time info.
1. Hit save button to create event in a calendar.

If such assumption is correct for your app then your model is likely to expose only properties and commands 
and bindings is likely to be a good fit. Generally speaking if your view only expose information by components 
of type A,B and your model only expose information with components of type X,Y then you can create
AX, BX, AY, BY bindings and use binding based architecture.

For most cases you assume A = Property, B = Events, X = Property and Y = Commands; Bindigns you are going 
to need Property<->Property, Event<->Command. Other pairs (Property<->Command and Event<->Property) does not make sense.

### Design ###

IBinding interface present a connection between source and target. Source and target are not defined at this level
as well as ways you can listen to their changes.

Complete binding connecting two things A and B contains following classes:

1. An object to access and listen to A.
1. An object to access and listen to B.
1. Binding object which store A and B and mediates betwee them.
1. Builder classes to provide fluent interface to cosntruct Binding object.
1. Methods to create A and B instances given source and target objects. 
1. helper classes which is used to configure and tweak objects from point 1, 2 and 3.

#### Property bindings ####

IPropertyBinding interface is a connection between two properties. Each property is represented by IProperty implementation. 
IPropery handle two concerns:

1. Access and change property value -> this is done via KVC system.
2. Subscribe to change events -> this is done via IPropertyBindingStrategy implementation.

All property implementations also implement IProperty<T> interface. Library code works with IProperty while 
user code works with more specialized IProperty<T>.

There is a special IProperty<T> implementation which combine converter methods with existing property and yield property 
of another type. Example:

```
Property<int> p = PropertyConversion.Convert<string, int>(source, _ => Convert.ToInt32(_), _ => _.ToString()) 
```

PropertyBindingBuilder class provide fluent interface to build bindings. Generally it provide builder methods to set all properties 
of IPropertyBinding object. Below two snippets of code are equivalent:

```
PropertyBindingBuilder
	.Create<int, string>(object, _ => _.StringProperty)
	.Convert<int, string>(_ => Convert.ToInt32(_), _ => _.ToString())
	.To(anotherObject, _ => _.IntProperty);
```

```
var b = new PropertyBinding();
var p = PropertyConversion.Convert<string, int>(
			object.Property(_ => _.StringProperty), 
			_ => Convert.ToInt32(_), 
			_ => _.ToString());
b.Source = p;
b.Target = anotherObject.Property(_ => _.IntProperty);
```

**Summary**

1. IProperty and IProperty<T> objects prepresenting source and target properties.
2. Same as above
3. PropertyBinding with properties Source and Target of type IProperty. 
Bind and Unbind methods connect/disconnect event handlers via IProperty interface
4. PropertyBindingBuilder
5. Extension methods for INotifyPropertyChanged and UIKit controls. Ex: model.Property( _ => _.MyProp) or textField.TextProperty()
6.1. IPropertyBindingStrategy - implement a way to listen to property changed. 
For example INotifyPropertyChangedStaretegy, KVCStrategy, NotificationCenterStartegy, etc
6.2. IKeyValueCoding implementations provided by KVC library are used to access properties. Custom implementations can be defined 
for specific object to tweak the way properties are accessed.


#### Command bindings ####

ICommandBinding is a connection between ICommand and IEventSource. ICommand enable/disable IEventSource and IEventSource fire a command.
Concrete implementation of IEventSource is EventHandlerSource which can be created like this:

```
public static class UIButtonEvents 
{
	public static readonly RuntimeEvent TouchUpInsideEvent = new RuntimeEvent(typeof(UIControl), "TouchUpInside");
	public static EventHandlerSource ClickSource(this UIButton button)
	{
		return new EventHandlerSource(TouchUpInsideEvent, button);
	}
}
```

and later on can be used like this:

```
Bindings.Command(command).To(button.ClickSource());
```

ParameterExtractor is a function to extract meaningful data from EventArgs of IEventSource and pass it to ICommand action argument.
Or for Android it can be used like this:

```
public static class AdapterViewBindings
{
    public static readonly RuntimeEvent ItemSelectedEvent = new RuntimeEvent (typeof(AdapterView), "ItemSelected");
    public static EventHandlerSource<T> ItemSelectedTarget<T>(this T view)
        where T : AdapterView   {
        return new EventHandlerSource<T>(ItemSelectedEvent, view)
        {
            SetEnabledAction = SetViewEnabled,
            ParameterExtractor = (args) => ((AdapterView.ItemSelectedEventArgs)args).Position
        };
    }
}
```

**Summary**

1. ICommand act as source on model side
2. IEventSource act as event trigger on UI side
3. ICommandBinding has Source and Target as well as several properties 

#### BindingList ####

Binding list is a collection of bindings which act as a single binding. BindingList is supposed to live in controller 
and be Bound/Unbound based on application lifecycle. For example iOS app might bind binding list in ViewWillAppear and 
Unbind in ViewWillDisappear.
