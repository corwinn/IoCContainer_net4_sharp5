Please, do no use this as a learning source; I wrote how not to do things at a
few places in the code, but I'm sure I failed to cover all cases. Thank you.

What is it?
===========
It is my attempt at understanding IoC Containers and DI.

What does it allow you to do?
=============================
You can abstract away object (called "service" in this context) construction,
especially when it depends on other "service"s that depend on other "services"s,
etc. The idea as far as I understand it, is to setup all your "service"s at
one place and query the "service"s at other places thus decoupling the two (
setup and initialization from usage) via interface contracts and a "service"
"pool" (IoC Container).


Credits
=======
IoCContainer_net4_sharp5  uses "NUnit" from "NUnit.Framework-3.12.0.zip" -
see "NUnit_license.txt".


Notice
======
All trademarks and/or service marks used in this project are the trademarks
and/or service marks of their respective owners.
