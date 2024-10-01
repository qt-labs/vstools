#include "mainwindow.h"
#include "ui_mainwindow.h"

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent)
    , ui(new Ui::MainWindow)
    , windowName(AnotherFancyStringClass("qmake project"))
{
    ui->setupUi(this);
    setWindowTitle(windowName);
}

MainWindow::~MainWindow()
{
    delete ui;
}

