EXTERNAL AcceptQuest()

Nuestras noches ya no son seguras, viajero. Una sombra se ha cernido sobre las ruinas del antiguo torreón. Un hechicero oscuro está reuniendo poder allí, y sus abominaciones corruptas ya han atacado a nuestros leñadores.

La guardia no se atreve a acercarse a su magia profana. Necesitamos a alguien con tu valor para poner fin a esta amenaza antes de que sea demasiado tarde. Acaba con el hechicero.

    + [Acepto el encargo. Me ocuparé de él.]
        ~ temp result = AcceptQuest()
        Que los dioses te protejan. El camino hacia el torreón es peligroso. Ten cuidado, su magia no es ningún juego.
        -> END

    + [Suena demasiado peligroso para mí.]
        Lo entiendo. Es una tarea para un verdadero héroe. Si cambias de opinión, la amenaza seguirá esperando.
        -> END
