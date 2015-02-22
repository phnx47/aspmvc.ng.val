(function () {
    'use strict';

    angular.module('ngval', []).directive('ngvalSubmit', [
        '$parse', function ($parse) {
            return {
                restrict: 'A',
                require: ['ngvalSubmit', '?form'],
                controller: [
                    '$scope', function ($scope) {
                        this.attempted = false;
                        var formController = null;

                        this.setAttempted = function () {
                            this.attempted = true;
                        };

                        this.clearForm = function () {
                            this.attempted = false;

                        };

                        this.setFormController = function (controller) {
                            formController = controller;
                        };

                    }
                ],
                compile: function () {
                    return {
                        pre: function (scope, formElement, attributes, controllers) {

                            var submitController = controllers[0];

                            var formController = (controllers.length > 1) ? controllers[1] : null;

                            submitController.setFormController(formController);

                            scope.ngvalForms = scope.ngvalForms || {};
                            scope.ngvalForms[attributes.name] = submitController;


                        },
                        post: function (scope, formElement, attributes, controllers) {

                            var submitController = controllers[0];
                            var formController = (controllers.length > 1) ? controllers[1] : null;
                            var fn = $parse(attributes.ngvalSubmit);

                            formElement.bind('submit', function () {
                                submitController.setAttempted();

                                if (!scope.$$phase) scope.$apply();
                                if (!formController.$valid) return false;

                                scope.$apply(function (event) {
                                    fn(scope, { $event: event });
                                });
                            });


                            scope.$watch(function () {
                                return scope.ngvalForms[attributes.name].attempted;
                            }, function (attempted) {

                                if (attempted === false) {

                                    formController.$setPristine();

                                    for (var i in formController) {

                                        var input = formController[i];

                                        if (input != undefined && input.hasOwnProperty('ngval') && input.ngval != undefined) {
                                            input.ngval.dirty = false;
                                            input.ngval.showError = false;
                                        }

                                    }
                                }
                            });
                        }
                    };
                }
            };
        }
    ]);

    angular.module('ngval').directive('ngvalField', [function () {
        return {
            restrict: 'A',
            require: ['^ngvalSubmit', 'ngModel'],
            link: function (scope, iElm, iAttrs, controllers) {

                var ngvalSubmit = scope.ngvalForms[iElm[0].form.name];
                var ngModel = controllers[1];


                ngModel.ngval = {
                    showError: false,
                    errors: [],
                    dirty: false
                };

                var messages = angular.fromJson(iAttrs.ngvalField);

                var getErrors = function () {
                    var errors = [];

                    for (var i = 0; i < messages.length; i++) {
                        if (ngModel.$error[messages[i].type]) {
                            errors.push({ validator: messages[i].type, message: messages[i].message });
                        }
                    }

                    return errors;
                };

                var setValidated = function () {
                    var errors = getErrors();
                    ngModel.ngval = {
                        errors: errors,
                        showError: (errors.length > 0 && ngvalSubmit.attempted),
                        dirty: (ngModel.$dirty && ngvalSubmit.attempted)
                    };

                };

                scope.$watch(function () {
                    return ngModel.$modelValue;
                }, function () {
                    if (ngvalSubmit.attempted) {
                        setValidated();
                    }
                });

                scope.$watch(function () {
                    return ngvalSubmit.attempted;
                }, function () {

                    if (ngvalSubmit.attempted) {
                        setValidated();
                    }
                });
            }
        };
    }]);

})();