from PyQt5.QtWidgets import *
from OpenGL.GLUT import *

import numpy as np

import MainWidget

def main():

    app = QApplication(sys.argv)
    window = MainWidget.MainWindow()
    window.setupUI()
    window.show()
    sys.exit(app.exec_())

main()